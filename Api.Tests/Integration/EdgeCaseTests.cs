using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Api.DTO;

namespace Api.Tests.Integration;

public sealed class EdgeCaseTests
{
    [Fact]
    public async Task GetMembers_WhenStokvelHasNoMembers_ReturnsEmptyCollection()
    {
        // testing an existing stokvel with no members is still a valid resource.
        // The API should return 200 OK with an empty list not 404.

        using var factory =
            new WebApplicationFactory<Program>();

        using var client =
            factory.CreateClient();

        // Create a new stokvel.
        // Because it has just been created, it has no members yet.
        var createRequest =
            new CreateStokvelRequest
            {
                Name = "Empty Test Stokvel",
                MonthlyContribution = 500m,
                CurrentPeriod = 1,
                TotalPeriods = 12,
                Rules = "Test stokvel with no members."
            };

        var createResponse =
            await client.PostAsJsonAsync(
                "/api/stokvels",
                createRequest);

        Assert.Equal(
            HttpStatusCode.Created,
            createResponse.StatusCode);

        var stokvel =
            await createResponse.Content
                .ReadFromJsonAsync<StokvelResponse>();

        Assert.NotNull(stokvel);

        // Ask for the membership collection.
        var response =
            await client.GetAsync(
                $"/api/stokvels/{stokvel.Id}/members");

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var members =
            await response.Content
                .ReadFromJsonAsync<List<UserResponse>>();

        Assert.NotNull(members);

        // Empty collection is the correct result.
        Assert.Empty(members);
    }


    [Fact]
    public async Task CreateContributionCycle_WithCycleNumberOne_Returns201()
    {
        // Cycle number 1 is the lowest value allowed by the validator
        // so this boundary value must still be accepted.

        using var factory =
            new WebApplicationFactory<Program>();

        using var client =
            factory.CreateClient();

        // Use an existing seeded stokvel.
        var stokvels =
            await client.GetFromJsonAsync<List<StokvelResponse>>(
                "/api/stokvels");

        Assert.NotNull(stokvels);
        Assert.NotEmpty(stokvels);

        var stokvel =
            stokvels.First();

        var request =
            new CreateContributionCycleRequest
            {
                // This is the important boundary value.
                CycleNumber = 1,

                TargetAmount = 1000m
            };

        var response =
            await client.PostAsJsonAsync(
                $"/api/stokvels/{stokvel.Id}/cycles",
                request);

        // 1 must be accepted because only values below 1 are invalid.
        Assert.Equal(
            HttpStatusCode.Created,
            response.StatusCode);

        var cycle =
            await response.Content
                .ReadFromJsonAsync<
                    ContributionCycleResponse>();

        Assert.NotNull(cycle);

        Assert.Equal(
            1,
            cycle.CycleNumber);

        Assert.Equal(
            1000m,
            cycle.TargetAmount);
    }


    [Fact]
    public async Task RecordContribution_WhenUserExistsButIsNotMember_Returns422ProblemJson()
    {
        // testing whether a request can be valid and reference real resources,
        // but the payment must still fail when the user does not belong to the Stokvel.

        using var factory =
            new WebApplicationFactory<Program>();

        using var client =
            factory.CreateClient();

        // Get an existing stokvel.
        var stokvels =
            await client.GetFromJsonAsync<List<StokvelResponse>>(
                "/api/stokvels");

        Assert.NotNull(stokvels);
        Assert.NotEmpty(stokvels);

        var stokvel =
            stokvels.First();

        // Create a completely valid User.
        // We deliberately do NOT add this User to the Stokvel.
        var createUserRequest =
            new CreateUserRequest
            {
                FullName = "Non Member Test User",
                Email = "nonmember@example.co.za",
                PhoneNumber = "0791234567"
            };

        var createUserResponse =
            await client.PostAsJsonAsync(
                "/api/users",
                createUserRequest);

        Assert.Equal(
            HttpStatusCode.Created,
            createUserResponse.StatusCode);

        var user =
            await createUserResponse.Content
                .ReadFromJsonAsync<UserResponse>();

        Assert.NotNull(user);

        // Get a real contribution cycle for the stokvel.
        var cycles =
            await client.GetFromJsonAsync<
                List<ContributionCycleResponse>>(
                    $"/api/stokvels/{stokvel.Id}/cycles");

        Assert.NotNull(cycles);
        Assert.NotEmpty(cycles);

        var cycle =
            cycles.First();

        // Everything about this request is structurally valid.
        var contributionRequest =
            new RecordContributionRequest
            {
                UserId = user.Id,
                ContributionCycleId = cycle.Id,
                Amount = stokvel.MonthlyContribution
            };

        using var request =
            new HttpRequestMessage(
                HttpMethod.Post,
                $"/api/stokvels/{stokvel.Id}/contributions");

        request.Headers.Add(
            "Idempotency-Key",
            "non-member-edge-case");

        request.Content =
            JsonContent.Create(contributionRequest);

        var response =
            await client.SendAsync(request);

        // The request passed validation but fails a business rule.
        Assert.Equal(
            HttpStatusCode.UnprocessableEntity,
            response.StatusCode);

        Assert.Equal(
            "application/problem+json",
            response.Content.Headers
                .ContentType?
                .MediaType);

        var body =
            await response.Content
                .ReadAsStringAsync();

        Assert.Contains(
            "\"status\":422",
            body);

        Assert.Contains(
            "correlationId",
            body);
    }
}