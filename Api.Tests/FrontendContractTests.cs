using System.Net;
using Api.Tests.TestSupport;

namespace Api.Tests;

[Collection("Postgres collection")]
public sealed class FrontendContractTests
{
    private readonly PostgresApiFactory _factory;

    public FrontendContractTests(
        PostgresApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task
        Frontend_preflight_is_allowed_with_credentials()
    {
        using var client =
            _factory.CreateClient();

        using var request =
            new HttpRequestMessage(
                HttpMethod.Options,
                "/api/v1/stokvels/" +
                $"{Guid.NewGuid()}/contributions");

        request.Headers.TryAddWithoutValidation(
            "Origin",
            "http://localhost:3000");

        request.Headers.TryAddWithoutValidation(
            "Access-Control-Request-Method",
            "POST");

        request.Headers.TryAddWithoutValidation(
            "Access-Control-Request-Headers",
            "authorization,content-type,idempotency-key");

        var response =
            await client.SendAsync(request);

        Assert.Equal(
            HttpStatusCode.NoContent,
            response.StatusCode);

        Assert.Equal(
            "http://localhost:3000",
            response.Headers
                .GetValues(
                    "Access-Control-Allow-Origin")
                .Single());

        Assert.Equal(
            "true",
            response.Headers
                .GetValues(
                    "Access-Control-Allow-Credentials")
                .Single());
    }

    [Fact]
    public async Task
        Unknown_origin_gets_no_cors_headers()
    {
        using var client =
            _factory.CreateClient();

        using var request =
            new HttpRequestMessage(
                HttpMethod.Options,
                "/api/v1/stokvels/" +
                $"{Guid.NewGuid()}/contributions");

        request.Headers.TryAddWithoutValidation(
            "Origin",
            "https://evil.example");

        request.Headers.TryAddWithoutValidation(
            "Access-Control-Request-Method",
            "POST");

        request.Headers.TryAddWithoutValidation(
            "Access-Control-Request-Headers",
            "authorization,content-type,idempotency-key");

        var response =
            await client.SendAsync(request);

        Assert.False(
            response.Headers.Contains(
                "Access-Control-Allow-Origin"));
    }
}