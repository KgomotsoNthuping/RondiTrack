using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Api.Tests.TestSupport;
using Infrastructure.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

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

    // ---------------------------------------------------------
    // CORS
    // ---------------------------------------------------------

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

        var allowedHeaders =
            string.Join(
                ",",
                response.Headers
                    .GetValues(
                        "Access-Control-Allow-Headers"));

        Assert.Contains(
            "idempotency-key",
            allowedHeaders,
            StringComparison.OrdinalIgnoreCase);
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

    [Fact]
    public async Task
        Stokvel_404_carries_frontend_cors_header()
    {
        using var client =
            _factory.CreateClient();

        var auth =
            await RegisterAndLoginAsync(
                client);

        Authenticate(
            client,
            auth.AccessToken);

        using var request =
            new HttpRequestMessage(
                HttpMethod.Get,
                $"/api/v1/stokvels/{Guid.NewGuid()}" +
                $"/contributions/{Guid.NewGuid()}");

        request.Headers.TryAddWithoutValidation(
            "Origin",
            "http://localhost:3000");

        var response =
            await client.SendAsync(request);

        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);

        Assert.Equal(
            "http://localhost:3000",
            response.Headers
                .GetValues(
                    "Access-Control-Allow-Origin")
                .Single());
    }

    // ---------------------------------------------------------
    // LOGIN RATE LIMIT
    // ---------------------------------------------------------

    [Fact]
    public async Task
        Login_is_rate_limited_with_429_problem_and_retry_after()
    {
        using var limitedApp =
            _factory.WithWebHostBuilder(
                builder =>
                {
                    builder.UseSetting(
                        "RateLimiting:Login:PermitLimit",
                        "2");

                    builder.UseSetting(
                        "RateLimiting:Login:WindowSeconds",
                        "60");
                });

        using var client =
            limitedApp.CreateClient();

        var login =
            new
            {
                email =
                    $"missing-{Guid.NewGuid():N}@example.test",

                password =
                    "WrongPassword!123"
            };

        var first =
            await client.PostAsJsonAsync(
                "/api/v1/auth/login",
                login);

        var second =
            await client.PostAsJsonAsync(
                "/api/v1/auth/login",
                login);

        var third =
            await client.PostAsJsonAsync(
                "/api/v1/auth/login",
                login);

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            first.StatusCode);

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            second.StatusCode);

        Assert.Equal(
            HttpStatusCode.TooManyRequests,
            third.StatusCode);

        Assert.True(
            third.Headers.Contains(
                "Retry-After"));

        Assert.Equal(
            "application/problem+json",
            third.Content.Headers
                .ContentType?
                .MediaType);

        var json =
            await third.Content
                .ReadAsStringAsync();

        using var document =
            JsonDocument.Parse(json);

        Assert.Equal(
            429,
            document.RootElement
                .GetProperty("status")
                .GetInt32());

        Assert.Equal(
            "Too Many Requests",
            document.RootElement
                .GetProperty("title")
                .GetString());

        Assert.True(
            document.RootElement
                .TryGetProperty(
                    "detail",
                    out var detail));

        Assert.False(
            string.IsNullOrWhiteSpace(
                detail.GetString()));

        Assert.True(
            document.RootElement
                .TryGetProperty(
                    "correlationId",
                    out var correlationId));

        Assert.False(
            string.IsNullOrWhiteSpace(
                correlationId.GetString()));
    }

    // ---------------------------------------------------------
    // MONEY RATE LIMIT + IDEMPOTENCY
    // ---------------------------------------------------------

    [Fact]
    public async Task
        Contribution_429_does_not_consume_idempotency_key()
    {
        using var limitedApp =
            _factory.WithWebHostBuilder(
                builder =>
                {
                    builder.UseSetting(
                        "RateLimiting:Contribution:PermitLimit",
                        "2");

                    builder.UseSetting(
                        "RateLimiting:Contribution:WindowSeconds",
                        "1");
                });

        using var client =
            limitedApp.CreateClient();

        var auth =
            await RegisterAndLoginAsync(
                client);

        Authenticate(
            client,
            auth.AccessToken);

        var data =
            await CreateMemberStokvelAsync(
                limitedApp.Services,
                auth.UserId);

        var contribution =
            new
            {
                userId =
                    auth.UserId,

                contributionCycleId =
                    data.CycleId,

                amount =
                    500m
            };

        // Consume the two permits without
        // recording the real contribution.
        for (var i = 0; i < 2; i++)
        {
            using var exhaust =
                new HttpRequestMessage(
                    HttpMethod.Post,
                    $"/api/v1/stokvels/" +
                    $"{Guid.NewGuid()}/contributions");

            exhaust.Headers.Add(
                "Idempotency-Key",
                $"exhaust-{Guid.NewGuid()}");

            exhaust.Content =
                JsonContent.Create(
                    contribution);

            await client.SendAsync(
                exhaust);
        }

        var key =
            $"financial-{Guid.NewGuid()}";

        using var rejected =
            new HttpRequestMessage(
                HttpMethod.Post,
                $"/api/v1/stokvels/" +
                $"{data.StokvelId}/contributions");

        rejected.Headers.Add(
            "Idempotency-Key",
            key);

        rejected.Content =
            JsonContent.Create(
                contribution);

        var rejectedResponse =
            await client.SendAsync(
                rejected);

        Assert.Equal(
            HttpStatusCode.TooManyRequests,
            rejectedResponse.StatusCode);

        Assert.True(
            rejectedResponse.Headers.Contains(
                "Retry-After"));

        // Wait for the short rate-limit window.
        await Task.Delay(1500);

        async Task<HttpResponseMessage>
            SendContributionAsync()
        {
            using var request =
                new HttpRequestMessage(
                    HttpMethod.Post,
                    $"/api/v1/stokvels/" +
                    $"{data.StokvelId}/contributions");

            request.Headers.Add(
                "Idempotency-Key",
                key);

            request.Content =
                JsonContent.Create(
                    contribution);

            return await client.SendAsync(
                request);
        }

        var firstSuccess =
            await SendContributionAsync();

        Assert.Equal(
            HttpStatusCode.Created,
            firstSuccess.StatusCode);

        var firstBody =
            await firstSuccess.Content
                .ReadAsStringAsync();

        var replay =
            await SendContributionAsync();

        Assert.Equal(
            HttpStatusCode.Created,
            replay.StatusCode);

        var replayBody =
            await replay.Content
                .ReadAsStringAsync();

        Assert.Equal(
            firstBody,
            replayBody);

        using var scope =
            limitedApp.Services
                .CreateScope();

        var db =
            scope.ServiceProvider
                .GetRequiredService<
                    RondiTrackDbContext>();

        var count =
            await db.Contributions
                .CountAsync(
                    row =>
                        row.UserId ==
                            auth.UserId &&
                        row.ContributionCycleId ==
                            data.CycleId);

        Assert.Equal(
            1,
            count);
    }

    [Fact]
    public async Task
        Money_limit_for_one_user_does_not_affect_another()
    {
        using var limitedApp =
            _factory.WithWebHostBuilder(
                builder =>
                {
                    builder.UseSetting(
                        "RateLimiting:Contribution:PermitLimit",
                        "1");

                    builder.UseSetting(
                        "RateLimiting:Contribution:WindowSeconds",
                        "60");
                });

        using var clientOne =
            limitedApp.CreateClient();

        using var clientTwo =
            limitedApp.CreateClient();

        var userOne =
            await RegisterAndLoginAsync(
                clientOne);

        var userTwo =
            await RegisterAndLoginAsync(
                clientTwo);

        Authenticate(
            clientOne,
            userOne.AccessToken);

        Authenticate(
            clientTwo,
            userTwo.AccessToken);

        async Task<HttpResponseMessage>
            SendAsync(
                HttpClient client,
                Guid userId)
        {
            using var request =
                new HttpRequestMessage(
                    HttpMethod.Post,
                    $"/api/v1/stokvels/" +
                    $"{Guid.NewGuid()}/contributions");

            request.Headers.Add(
                "Idempotency-Key",
                Guid.NewGuid()
                    .ToString());

            request.Content =
                JsonContent.Create(
                    new
                    {
                        userId,

                        contributionCycleId =
                            Guid.NewGuid(),

                        amount =
                            500m
                    });

            return await client.SendAsync(
                request);
        }

        // User one consumes their permit.
        await SendAsync(
            clientOne,
            userOne.UserId);

        // User one's next request is blocked.
        var userOneSecond =
            await SendAsync(
                clientOne,
                userOne.UserId);

        Assert.Equal(
            HttpStatusCode.TooManyRequests,
            userOneSecond.StatusCode);

        // User two has a separate partition,
        // so they still get their own permit.
        var userTwoFirst =
            await SendAsync(
                clientTwo,
                userTwo.UserId);

        Assert.NotEqual(
            HttpStatusCode.TooManyRequests,
            userTwoFirst.StatusCode);
    }

    // ---------------------------------------------------------
    // MEMBER HOME SCREEN
    // ---------------------------------------------------------

    [Fact]
    public async Task
        Member_of_two_stokvels_sees_both_roles()
    {
        using var client =
            _factory.CreateClient();

        var auth =
            await RegisterAndLoginAsync(
                client);

        Authenticate(
            client,
            auth.AccessToken);

        await CreateMemberStokvelAsync(
            _factory.Services,
            auth.UserId,
            "Member");

        await CreateMemberStokvelAsync(
            _factory.Services,
            auth.UserId,
            "Admin");

        var response =
            await client.GetAsync(
                "/api/v1/me/home");

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var json =
            await response.Content
                .ReadAsStringAsync();

        using var document =
            JsonDocument.Parse(json);

        var stokvels =
            document.RootElement
                .GetProperty(
                    "stokvels");

        Assert.Equal(
            2,
            stokvels.GetArrayLength());

        var roles =
            stokvels
                .EnumerateArray()
                .Select(
                    item =>
                        item.GetProperty(
                                "role")
                            .GetString())
                .ToList();

        Assert.Contains(
            "Member",
            roles);

        Assert.Contains(
            "Admin",
            roles);

        foreach (var stokvel
                 in stokvels.EnumerateArray())
        {
            Assert.True(
                stokvel.TryGetProperty(
                    "name",
                    out _));

            Assert.True(
                stokvel.TryGetProperty(
                    "currentCycle",
                    out _));

            Assert.True(
                stokvel.TryGetProperty(
                    "paidThisCycleInCents",
                    out _));

            Assert.True(
                stokvel.TryGetProperty(
                    "stillOwedInCents",
                    out _));

            Assert.True(
                stokvel.TryGetProperty(
                    "nextPayoutDateUtc",
                    out _));
        }
    }

    [Fact]
    public async Task
        User_with_no_stokvels_gets_empty_home()
    {
        using var client =
            _factory.CreateClient();

        var auth =
            await RegisterAndLoginAsync(
                client);

        Authenticate(
            client,
            auth.AccessToken);

        var response =
            await client.GetAsync(
                "/api/v1/me/home");

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var json =
            await response.Content
                .ReadAsStringAsync();

        using var document =
            JsonDocument.Parse(json);

        Assert.Equal(
            0,
            document.RootElement
                .GetProperty("stokvels")
                .GetArrayLength());
    }

    // ---------------------------------------------------------
    // ERROR CONTRACT + STRICT JSON NUMBERS
    // ---------------------------------------------------------

    [Fact]
    public async Task
        Validation_failure_returns_errors_map_with_field_name()
    {
        using var client =
            _factory.CreateClient();

        var auth =
            await RegisterAndLoginAsync(
                client);

        Authenticate(
            client,
            auth.AccessToken);

        var data =
            await CreateMemberStokvelAsync(
                _factory.Services,
                auth.UserId);

        using var request =
            new HttpRequestMessage(
                HttpMethod.Post,
                $"/api/v1/stokvels/" +
                $"{data.StokvelId}/contributions");

        request.Headers.Add(
            "Idempotency-Key",
            Guid.NewGuid()
                .ToString());

        request.Content =
            JsonContent.Create(
                new
                {
                    userId =
                        auth.UserId,

                    contributionCycleId =
                        data.CycleId,

                    amount =
                        0m
                });

        var response =
            await client.SendAsync(
                request);

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);

        Assert.Equal(
            "application/problem+json",
            response.Content.Headers
                .ContentType?
                .MediaType);

        var json =
            await response.Content
                .ReadAsStringAsync();

        using var document =
            JsonDocument.Parse(json);

        Assert.True(
            document.RootElement
                .TryGetProperty(
                    "errors",
                    out var errors));

        Assert.True(
            errors.TryGetProperty(
                "amount",
                out _));

        Assert.True(
            document.RootElement
                .TryGetProperty(
                    "correlationId",
                    out _));
    }

    [Fact]
    public async Task
        Number_sent_as_string_returns_400_without_internal_type()
    {
        using var client =
            _factory.CreateClient();

        var auth =
            await RegisterAndLoginAsync(
                client);

        Authenticate(
            client,
            auth.AccessToken);

        var data =
            await CreateMemberStokvelAsync(
                _factory.Services,
                auth.UserId);

        var body =
            $$"""
            {
              "userId": "{{auth.UserId}}",
              "contributionCycleId": "{{data.CycleId}}",
              "amount": "500"
            }
            """;

        using var request =
            new HttpRequestMessage(
                HttpMethod.Post,
                $"/api/v1/stokvels/" +
                $"{data.StokvelId}/contributions");

        request.Headers.Add(
            "Idempotency-Key",
            Guid.NewGuid()
                .ToString());

        request.Content =
            new StringContent(
                body,
                Encoding.UTF8,
                "application/json");

        var response =
            await client.SendAsync(
                request);

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);

        var responseBody =
            await response.Content
                .ReadAsStringAsync();

        Assert.DoesNotContain(
            "RecordContributionRequest",
            responseBody);

        Assert.DoesNotContain(
            "System.Text.Json",
            responseBody);

        Assert.DoesNotContain(
            "JsonException",
            responseBody);
    }

    // ---------------------------------------------------------
    // VERSIONED REFRESH COOKIE
    // ---------------------------------------------------------

    [Fact]
    public async Task
        Refresh_succeeds_at_versioned_path()
    {
        using var client =
            _factory.CreateClient(
                new WebApplicationFactoryClientOptions
                {
                    BaseAddress =
                        new Uri(
                            "https://localhost"),

                    HandleCookies =
                        true
                });

        await RegisterAndLoginAsync(
            client);

        var response =
            await client.PostAsync(
                "/api/v1/auth/refresh",
                null);

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var json =
            await response.Content
                .ReadAsStringAsync();

        using var document =
            JsonDocument.Parse(json);

        Assert.True(
            document.RootElement
                .TryGetProperty(
                    "accessToken",
                    out var token));

        Assert.False(
            string.IsNullOrWhiteSpace(
                token.GetString()));
    }

    // ---------------------------------------------------------
    // HELPERS
    // ---------------------------------------------------------

    private async Task<
        (Guid UserId, string AccessToken)>
        RegisterAndLoginAsync(
            HttpClient client)
    {
        var email =
            $"frontend-{Guid.NewGuid():N}" +
            "@example.test";

        const string password =
            "FrontendPassword!123";

        var registerResponse =
            await client.PostAsJsonAsync(
                "/api/v1/auth/register",
                new
                {
                    fullName =
                        "Frontend Test User",

                    email,

                    phoneNumber =
                        "0821234567",

                    password
                });

        Assert.Equal(
            HttpStatusCode.Created,
            registerResponse.StatusCode);

        var registerJson =
            await registerResponse.Content
                .ReadAsStringAsync();

        using var registerDocument =
            JsonDocument.Parse(
                registerJson);

        var userId =
            registerDocument.RootElement
                .GetProperty("id")
                .GetGuid();

        var loginResponse =
            await client.PostAsJsonAsync(
                "/api/v1/auth/login",
                new
                {
                    email,
                    password
                });

        Assert.Equal(
            HttpStatusCode.OK,
            loginResponse.StatusCode);

        var loginJson =
            await loginResponse.Content
                .ReadAsStringAsync();

        using var loginDocument =
            JsonDocument.Parse(
                loginJson);

        var accessToken =
            loginDocument.RootElement
                .GetProperty(
                    "accessToken")
                .GetString();

        Assert.False(
            string.IsNullOrWhiteSpace(
                accessToken));

        return (
            userId,
            accessToken!);
    }

    private static void Authenticate(
        HttpClient client,
        string accessToken)
    {
        client.DefaultRequestHeaders
            .Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                accessToken);
    }

    private async Task<
        (Guid StokvelId, Guid CycleId)>
        CreateMemberStokvelAsync(
            IServiceProvider services,
            Guid userId,
            string role = "Member",
            decimal amount = 500m)
    {
        var stokvelId =
            Guid.NewGuid();

        var cycleId =
            Guid.NewGuid();

        var name =
            "Test Stokvel " +
            stokvelId
                .ToString("N")[..6];

        string? rules =
            null;

        using var scope =
            services.CreateScope();

        var db =
            scope.ServiceProvider
                .GetRequiredService<
                    RondiTrackDbContext>();

        await db.Database
            .ExecuteSqlInterpolatedAsync(
                $"""
                INSERT INTO "Stokvels"
                (
                    "Id",
                    "Name",
                    "MonthlyContribution",
                    "CurrentPeriod",
                    "TotalPeriods",
                    "Rules"
                )
                VALUES
                (
                    {stokvelId},
                    {name},
                    {amount},
                    {1},
                    {12},
                    {rules}
                );
                """);

        await db.Database
            .ExecuteSqlInterpolatedAsync(
                $"""
                INSERT INTO "StokvelMembers"
                (
                    "UserId",
                    "StokvelId",
                    "JoinedAtUtc",
                    "Role"
                )
                VALUES
                (
                    {userId},
                    {stokvelId},
                    {DateTime.UtcNow},
                    {role}
                );
                """);

        await db.Database
            .ExecuteSqlInterpolatedAsync(
                $"""
                INSERT INTO "ContributionCycles"
                (
                    "Id",
                    "StokvelId",
                    "CycleNumber",
                    "TargetAmount",
                    "Status"
                )
                VALUES
                (
                    {cycleId},
                    {stokvelId},
                    {1},
                    {amount},
                    {0}
                );
                """);

        return (
            stokvelId,
            cycleId);
    }
}