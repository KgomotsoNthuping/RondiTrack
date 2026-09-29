using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Api.DTO;

namespace Api.Tests.Integration;

public sealed class ContributionEndpointTests
{
    [Fact]
    public async Task RecordContribution_WhenSameRequestIsRetried_ReturnsIdenticalResponse()
    {
        // testing the exact same contribution with the same Idempotency-Key 
        // must return the original contribution instead of recording the payment twice.

        using var factory =
            new WebApplicationFactory<Program>();

        using var client =
            factory.CreateClient();

        // Get one of the seeded stokvels.
        var stokvels =
            await client.GetFromJsonAsync<List<StokvelResponse>>(
                "/api/stokvels");

        Assert.NotNull(stokvels);
        Assert.NotEmpty(stokvels);

        var stokvel =
            stokvels.First();

        // Get an existing member of that stokvel.
        var members =
            await client.GetFromJsonAsync<List<UserResponse>>(
                $"/api/stokvels/{stokvel.Id}/members");

        Assert.NotNull(members);
        Assert.NotEmpty(members);

        var member =
            members.First();

        // Get an existing contribution cycle.
        var cycles =
            await client.GetFromJsonAsync<
                List<ContributionCycleResponse>>(
                    $"/api/stokvels/{stokvel.Id}/cycles");

        Assert.NotNull(cycles);
        Assert.NotEmpty(cycles);

        var cycle =
            cycles.First();

        var request =
            new RecordContributionRequest
            {
                UserId = member.Id,
                ContributionCycleId = cycle.Id,
                Amount = stokvel.MonthlyContribution
            };

        const string idempotencyKey =
            "same-contribution-request-test";

        // Send the contribution for the first time.
        using var firstRequest =
            new HttpRequestMessage(
                HttpMethod.Post,
                $"/api/stokvels/{stokvel.Id}/contributions");

        firstRequest.Headers.Add(
            "Idempotency-Key",
            idempotencyKey);

        firstRequest.Content =
            JsonContent.Create(request);

        var firstResponse =
            await client.SendAsync(firstRequest);

        Assert.Equal(
            HttpStatusCode.Created,
            firstResponse.StatusCode);

        var firstBody =
            await firstResponse.Content
                .ReadAsStringAsync();

        // Send exactly the same request again
        // using exactly the same Idempotency-Key.
        using var secondRequest =
            new HttpRequestMessage(
                HttpMethod.Post,
                $"/api/stokvels/{stokvel.Id}/contributions");

        secondRequest.Headers.Add(
            "Idempotency-Key",
            idempotencyKey);

        secondRequest.Content =
            JsonContent.Create(request);

        var secondResponse =
            await client.SendAsync(secondRequest);

        Assert.Equal(
            HttpStatusCode.Created,
            secondResponse.StatusCode);

        var secondBody =
            await secondResponse.Content
                .ReadAsStringAsync();

        // The second response must be the exact original result.
        Assert.Equal(
            firstBody,
            secondBody);
    }


    [Fact]
    public async Task RecordContribution_WhenIdempotencyKeyIsReusedWithDifferentPayload_Returns409ProblemJson()
    {
        // The same Idempotency-Key cannot be reused for a different
        // contribution request. A changed payload must return 409 Conflict.

        using var factory =
            new WebApplicationFactory<Program>();

        using var client =
            factory.CreateClient();

        var stokvels =
            await client.GetFromJsonAsync<List<StokvelResponse>>(
                "/api/stokvels");

        Assert.NotNull(stokvels);
        Assert.NotEmpty(stokvels);

        var stokvel =
            stokvels.First();

        var members =
            await client.GetFromJsonAsync<List<UserResponse>>(
                $"/api/stokvels/{stokvel.Id}/members");

        Assert.NotNull(members);
        Assert.NotEmpty(members);

        var member =
            members.First();

        var cycles =
            await client.GetFromJsonAsync<
                List<ContributionCycleResponse>>(
                    $"/api/stokvels/{stokvel.Id}/cycles");

        Assert.NotNull(cycles);
        Assert.NotEmpty(cycles);

        var cycle =
            cycles.First();

        const string idempotencyKey =
            "different-payload-test";

        var originalRequest =
            new RecordContributionRequest
            {
                UserId = member.Id,
                ContributionCycleId = cycle.Id,
                Amount = stokvel.MonthlyContribution
            };

        // First request is valid and should succeed.
        using var firstRequest =
            new HttpRequestMessage(
                HttpMethod.Post,
                $"/api/stokvels/{stokvel.Id}/contributions");

        firstRequest.Headers.Add(
            "Idempotency-Key",
            idempotencyKey);

        firstRequest.Content =
            JsonContent.Create(originalRequest);

        var firstResponse =
            await client.SendAsync(firstRequest);

        Assert.Equal(
            HttpStatusCode.Created,
            firstResponse.StatusCode);

        // Reuse the same key but change the contribution amount.
        var changedRequest =
            new RecordContributionRequest
            {
                UserId = member.Id,
                ContributionCycleId = cycle.Id,
                Amount =
                    stokvel.MonthlyContribution + 100m
            };

        using var secondRequest =
            new HttpRequestMessage(
                HttpMethod.Post,
                $"/api/stokvels/{stokvel.Id}/contributions");

        secondRequest.Headers.Add(
            "Idempotency-Key",
            idempotencyKey);

        secondRequest.Content =
            JsonContent.Create(changedRequest);

        var secondResponse =
            await client.SendAsync(secondRequest);

        // Same key + different payload must be rejected.
        Assert.Equal(
            HttpStatusCode.Conflict,
            secondResponse.StatusCode);

        // All API failures should use the centralized problem+json shape.
        Assert.Equal(
            "application/problem+json",
            secondResponse.Content.Headers
                .ContentType?
                .MediaType);

        var body =
            await secondResponse.Content
                .ReadAsStringAsync();

        Assert.Contains(
            "\"status\":409",
            body);

        Assert.Contains(
            "correlationId",
            body);
    }
}