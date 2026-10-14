using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Api.DTO;
using Api.Tests.TestSupport;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Diagnostics.Metrics;
using Api.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Api.Tests.Integration;

[Collection("Postgres collection")]
public sealed class ObservabilityTests
{
    private const string TraceId =
        "4bf92f3577b34da6a3ce929d0e0e4736";

    private const string TraceParent =
        "00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-01";

    private readonly PostgresApiFactory _factory;
    private readonly HttpClient _client;

    public ObservabilityTests(
        PostgresApiFactory factory)
    {
        _factory = factory;

        _client = factory.CreateClient(
            new WebApplicationFactoryClientOptions
            {
                BaseAddress =
                    new Uri("https://localhost")
            });
    }

    [Fact]
    public async Task
        Liveness_answers_without_a_token()
    {
        var response =
            await _client.GetAsync(
                "/health/live");

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var body =
            await response.Content
                .ReadAsStringAsync();

        Assert.Equal(
            "Healthy",
            body.Trim());
    }

    [Fact]
    public async Task
        Readiness_answers_without_a_token()
    {
        var response =
            await _client.GetAsync(
                "/health/ready");

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var body =
            await response.Content
                .ReadAsStringAsync();

        Assert.Equal(
            "Healthy",
            body.Trim());
    }

    [Fact]
    public async Task
        Anonymous_401_uses_callers_trace_id()
    {
        using var request =
            new HttpRequestMessage(
                HttpMethod.Get,
                "/api/v1/users");

        request.Headers.TryAddWithoutValidation(
            "traceparent",
            TraceParent);

        var response =
            await _client.SendAsync(request);

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);

        await AssertCorrelationIdAsync(
            response);
    }

    [Fact]
    public async Task Non_member_404_uses_callers_trace_id()
    {
        var accessToken =
            await RegisterAndLoginAsync();

        using var request =
            new HttpRequestMessage(
                HttpMethod.Get,
                $"/api/v1/stokvels/{Guid.NewGuid()}" +
                $"/contributions/{Guid.NewGuid()}");

        request.Headers.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                accessToken);

        request.Headers.TryAddWithoutValidation(
            "traceparent",
            TraceParent);

        var response =
            await _client.SendAsync(request);

        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);

        await AssertCorrelationIdAsync(
            response);
    }

    [Fact]
    public async Task
        Payout_counter_records_success()
    {
        var data =
            await TestData.CreateAsync(
                _factory.Services,
                memberCount: 2);

        var measurements =
            new List<(long Value, string? Outcome)>();

        using var listener =
            new MeterListener();

        listener.InstrumentPublished =
            (instrument, meterListener) =>
            {
                if (instrument.Meter.Name ==
                        Api.Observability.Metrics.MeterName &&
                    instrument.Name ==
                        "ronditrack.payouts.processed")
                {
                    meterListener
                        .EnableMeasurementEvents(
                            instrument);
                }
            };

        listener.SetMeasurementEventCallback<long>(
            (instrument,
            measurement,
            tags,
            state) =>
            {
                string? outcome = null;

                foreach (var tag in tags)
                {
                    if (tag.Key == "outcome")
                    {
                        outcome =
                            tag.Value?.ToString();
                    }
                }

                measurements.Add(
                    (measurement, outcome));
            });

        listener.Start();

        using var scope =
            _factory.Services.CreateScope();

        var payoutService =
            scope.ServiceProvider
                .GetRequiredService<
                    IPayoutService>();

        await payoutService
            .ProcessPayoutAsync(
                data.StokvelId,
                data.CycleId);

        Assert.Contains(
            measurements,
            measurement =>
                measurement.Value == 1 &&
                measurement.Outcome ==
                    "success");
    }

    private async Task<string>
        RegisterAndLoginAsync()
    {
        var email =
            $"trace-{Guid.NewGuid():N}@example.test";

        const string password =
            "TestPassword!123";

        var register =
            new RegisterRequest
            {
                FullName = "Trace Test User",
                Email = email,
                PhoneNumber = "0821234567",
                Password = password
            };

        var registerResponse =
            await _client.PostAsJsonAsync(
                "/api/v1/auth/register",
                register);

        Assert.Equal(
            HttpStatusCode.Created,
            registerResponse.StatusCode);

        var login =
            new LoginRequest
            {
                Email = email,
                Password = password
            };

        var loginResponse =
            await _client.PostAsJsonAsync(
                "/api/v1/auth/login",
                login);

        Assert.Equal(
            HttpStatusCode.OK,
            loginResponse.StatusCode);

        var auth =
            await loginResponse.Content
                .ReadFromJsonAsync<AuthResponse>();

        Assert.NotNull(auth);

        return auth.AccessToken;
    }

    private static async Task
        AssertCorrelationIdAsync(
            HttpResponseMessage response)
    {
        var json =
            await response.Content
                .ReadAsStringAsync();

        using var document =
            JsonDocument.Parse(json);

        var correlationId =
            document.RootElement
                .GetProperty("correlationId")
                .GetString();

        Assert.Equal(
            TraceId,
            correlationId);
    }
}
