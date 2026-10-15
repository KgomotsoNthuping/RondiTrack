using System.Net;
using System.Net.Http.Json;
using Api.DTO;
using Api.Tests.TestSupport;

namespace Api.Tests.Integration;

[Collection("Postgres collection")]
public sealed class ContributionEndpointTests
{
    private readonly PostgresApiFactory _factory;

    public ContributionEndpointTests(PostgresApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task RecordContribution_WhenSameRequestIsRetried_ReturnsIdenticalResponse()
    {
        // Create a genuine member and a cycle with no existing contributions.
        var data = await TestData.CreateAsync(
            _factory.Services,
            memberCount: 1,
            createContributions: false);

        using var client = _factory.CreateAuthenticatedClient(
            data.UserIds.First());

        var request = new RecordContributionRequest
        {
            UserId = data.UserIds.First(),
            ContributionCycleId = data.CycleId,
            Amount = 500m
        };

        var idempotencyKey = $"same-contribution-{Guid.NewGuid():N}";
        var endpoint = $"/api/v1/stokvels/{data.StokvelId}/contributions";

        async Task<HttpResponseMessage> SendAsync()
        {
            using var message = new HttpRequestMessage(HttpMethod.Post, endpoint);
            message.Headers.Add("Idempotency-Key", idempotencyKey);
            message.Content = JsonContent.Create(request);
            return await client.SendAsync(message);
        }

        using var first = await SendAsync();
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        var firstBody = await first.Content.ReadAsStringAsync();

        using var second = await SendAsync();
        Assert.Equal(HttpStatusCode.Created, second.StatusCode);
        var secondBody = await second.Content.ReadAsStringAsync();

        // A replay must be byte-for-byte identical, not merely numerically equal.
        Assert.Equal(firstBody, secondBody);
    }

    [Fact]
    public async Task RecordContribution_WhenIdempotencyKeyIsReusedWithDifferentPayload_Returns409ProblemJson()
    {
        var data = await TestData.CreateAsync(
            _factory.Services,
            memberCount: 1,
            createContributions: false);

        using var client = _factory.CreateAuthenticatedClient(
            data.UserIds.First());

        var endpoint = $"/api/v1/stokvels/{data.StokvelId}/contributions";
        var idempotencyKey = $"different-payload-{Guid.NewGuid():N}";

        async Task<HttpResponseMessage> SendAsync(decimal amount)
        {
            using var message = new HttpRequestMessage(HttpMethod.Post, endpoint);
            message.Headers.Add("Idempotency-Key", idempotencyKey);
            message.Content = JsonContent.Create(new RecordContributionRequest
            {
                UserId = data.UserIds.First(),
                ContributionCycleId = data.CycleId,
                Amount = amount
            });
            return await client.SendAsync(message);
        }

        using var original = await SendAsync(500m);
        Assert.Equal(HttpStatusCode.Created, original.StatusCode);

        using var changed = await SendAsync(600m);
        Assert.Equal(HttpStatusCode.Conflict, changed.StatusCode);
        Assert.Equal("application/problem+json", changed.Content.Headers.ContentType?.MediaType);
        var body = await changed.Content.ReadAsStringAsync();
        Assert.Contains("\"status\":409", body);
        Assert.Contains("correlationId", body);
    }
}
