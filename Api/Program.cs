using Npgsql;
using OpenTelemetry;
using System.Text;
using Api.Auth;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Api.Data;
using Scalar.AspNetCore;
using FluentValidation;
using Api.Services;
using Api.Validation;
using Api.ErrorHandling;
using Microsoft.EntityFrameworkCore;
using Domain.Repositories;
using Infrastructure.Data;
using Domain.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Api.Observability;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Api.Common;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

var allowedOrigins = builder.Configuration
        .GetSection("Cors:AllowedOrigins")
        .Get<string[]>() ?? [];

var jwtSection = builder.Configuration.GetSection("Jwt");

var jwtOptions = jwtSection.Get<JwtOptions>() ?? throw new InvalidOperationException("JWT configuration is missing.");

if (string.IsNullOrWhiteSpace(jwtOptions.SigningKey) || Encoding.UTF8.GetByteCount(jwtOptions.SigningKey) < 32)
{
    throw new InvalidOperationException("JWT signing key is missing or too short. " +
        "Set Jwt:SigningKey using dotnet user-secrets.");
}

builder.Services.AddSingleton(jwtOptions);

builder.Services.AddControllers(options =>
{
    options.Filters.AddService<ValidationFilter>();
})
.ConfigureApiBehaviorOptions(options =>
{
    options.SuppressModelStateInvalidFilter = false;
})
.AddJsonOptions(options =>
{
    options.AllowInputFormatterExceptionMessages =
        false;

    options.JsonSerializerOptions.NumberHandling =
        JsonNumberHandling.Strict;
});

builder.Services.ConfigureHttpJsonOptions(
    options =>
    {
        options.SerializerOptions.NumberHandling =
            JsonNumberHandling.Strict;
    });

builder.Services.AddValidatorsFromAssemblyContaining<CreateUserRequestValidator>(); 

builder.Services.AddScoped<ValidationFilter>();

builder.Services.AddOpenApi();

var connectionString = builder.Configuration.GetConnectionString("RondiTrack") ??
     throw new InvalidOperationException("The RondiTrack database connection string is missing.");

builder.Services.AddDbContext<RondiTrackDbContext>(options =>
    {
        options.UseNpgsql(
            connectionString,
            npgsqlOptions =>
            {
                npgsqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 3,
                    maxRetryDelay: TimeSpan.FromSeconds(5),
                    errorCodesToAdd: null);
            });
    });

// Enables standardized Problem Details output.
builder.Services.AddProblemDetails(options =>
{
    options.CustomizeProblemDetails = context =>
    {
        context.ProblemDetails.Extensions["correlationId"] =
            Correlation.GetId(context.HttpContext);

        context.ProblemDetails.Instance ??=
            context.HttpContext.Request.Path;
    };
});

// One centralized exception handler.
builder.Services.AddExceptionHandler<ExceptionHandler>();

// To use inmemory store instance 
builder.Services.AddScoped<
    ITrackStore,
    EfTrackStore>();

// Stores Idempotency-Key results in memory.
builder.Services.AddSingleton<
    IIdempotency,
    InMemoryIdempotency>();

// Contains RondiTrack business decisions.
builder.Services.AddScoped<
    ITrackService,
    TrackService>();

builder.Services.AddScoped<
    ICycleCloseRepository,
    EfCycleCloseRepository>();

// Processes payouts inside an explicit database transaction.
builder.Services.AddScoped<
    IPayoutService,
    PayoutService>();

builder.Services.AddScoped<
    IPayoutTransaction,
    NoOpPayoutTransaction>();

builder.Services.AddScoped<
    IPayoutService,
    PayoutService>();

builder.Services.AddScoped<
    IContributionQueryService,
    ContributionQueryService>();

builder.Services.AddScoped<
    IStokvelMemberQueryService,
    StokvelMemberQueryService>();

builder.Services.AddScoped<
    IPasswordHasher<User>,
    PasswordHasher<User>>();

builder.Services.AddScoped<AuthService>();

builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .Build();

    options.AddPolicy(
        Policies.StokvelMember,
        policy =>
        {
            policy.RequireAuthenticatedUser();

            policy.AddRequirements(
                new StokvelAccessRequirement(
                    StokvelAccessLevel.Member));
        });

    options.AddPolicy(
        Policies.StokvelAdmin,
        policy =>
        {
            policy.RequireAuthenticatedUser();

            policy.AddRequirements(
                new StokvelAccessRequirement(
                    StokvelAccessLevel.Admin));
        });
});

builder.Services.AddSingleton<
    TokenService>();

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;

        options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = jwtOptions.Issuer,

                ValidateAudience = true,
                ValidAudience = jwtOptions.Audience,

                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SigningKey)),

                ValidateLifetime = true,

                ClockSkew = TimeSpan.FromSeconds(30),

                 RoleClaimType = "role",

                ValidAlgorithms = [ SecurityAlgorithms.HmacSha256 ]
            };
    });

builder.Services.AddScoped<
    IRefreshTokenRepository,
    EfRefreshTokenRepository>();

builder.Services.AddScoped<
    IAuthorizationHandler,
    StokvelAuthorizationHandler>();

builder.Services.AddScoped<
    StokvelAccessAuthorizer>();

builder.Services.AddSingleton<
    Metrics>();

var openTelemetry = builder.Services
        .AddOpenTelemetry()
        .ConfigureResource(resource => resource.AddService(Telemetry.ServiceName))
        .WithTracing(tracing =>
        {
            tracing
                .AddSource(Telemetry.ActivitySourceName)
                .AddAspNetCoreInstrumentation(options =>
                    {
                        options.Filter = httpContext => !httpContext
                                    .Request
                                    .Path
                                    .StartsWithSegments("/health");
                    })
                .AddNpgsql();
        })
        .WithMetrics(metrics =>
        {
            metrics
                .AddAspNetCoreInstrumentation()
                .AddMeter("System.Runtime")
                .AddMeter(Metrics.MeterName);
        })
        .WithLogging();

if (!string.IsNullOrWhiteSpace(
        builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]))
{
    openTelemetry.UseOtlpExporter();
}

builder.Services
    .AddHealthChecks()
    .AddDbContextCheck<RondiTrackDbContext>("database",tags: ["ready"]);

builder.Services.Configure< HealthCheckServiceOptions>(options =>
    {
        foreach (var registration in options.Registrations)
        {
            registration.Timeout = TimeSpan.FromSeconds(3);
        }
    });

builder.Services.AddCors(options =>
    {
        options.AddPolicy(
            CorsPolicies.Frontend,
            policy =>
            {
                policy
                    .WithOrigins(
                        allowedOrigins)
                    .AllowCredentials()
                    .WithMethods(
                        "GET",
                        "POST",
                        "PUT",
                        "DELETE")
                    .WithHeaders(
                        "Authorization",
                        "Content-Type",
                        "Idempotency-Key",
                        "traceparent")
                    .WithExposedHeaders(
                        "Retry-After")
                    .SetPreflightMaxAge(
                        TimeSpan.FromMinutes(10));
            });
    });

var loginPermitLimit =
    builder.Configuration.GetValue(
        "RateLimiting:Login:PermitLimit",
        5);

var loginWindowSeconds =
    builder.Configuration.GetValue(
        "RateLimiting:Login:WindowSeconds",
        60);

var loginWindow =
    TimeSpan.FromSeconds(
        loginWindowSeconds);

var contributionPermitLimit =
    builder.Configuration.GetValue(
        "RateLimiting:Contribution:PermitLimit",
        30);

var contributionWindow =
    TimeSpan.FromSeconds(
        builder.Configuration.GetValue(
            "RateLimiting:Contribution:WindowSeconds",
            60));

var payoutPermitLimit =
    builder.Configuration.GetValue(
        "RateLimiting:Payout:PermitLimit",
        5);

var payoutWindow =
    TimeSpan.FromSeconds(
        builder.Configuration.GetValue(
            "RateLimiting:Payout:WindowSeconds",
            60));

builder.Services.AddRateLimiter(
    options =>
    {
        options.AddPolicy(
            RateLimitPolicies.Login,
            httpContext =>
                RateLimitPartition
                    .GetFixedWindowLimiter(
                        partitionKey:
                            httpContext.Connection
                                .RemoteIpAddress?
                                .ToString()
                            ?? "unknown",

                        factory: _ =>
                            new FixedWindowRateLimiterOptions
                            {
                                PermitLimit =
                                    loginPermitLimit,

                                Window =
                                    loginWindow,

                                QueueLimit = 0,

                                AutoReplenishment = true
                            }));

        options.RejectionStatusCode =
            StatusCodes.Status429TooManyRequests;

        options.AddPolicy(
            RateLimitPolicies.Contribution,
            httpContext =>
            {
                var userId =
                    httpContext.User
                        .FindFirst("sub")?
                        .Value
                    ?? "anonymous";

                return RateLimitPartition
                    .GetFixedWindowLimiter(
                        partitionKey: userId,
                        factory: _ =>
                            new FixedWindowRateLimiterOptions
                            {
                                PermitLimit =
                                    contributionPermitLimit,

                                Window =
                                    contributionWindow,

                                QueueLimit = 0,

                                AutoReplenishment = true
                            });
            });

            options.AddPolicy(
                RateLimitPolicies.Payout,
                httpContext =>
                {
                    var userId =
                        httpContext.User
                            .FindFirst("sub")?
                            .Value
                        ?? "anonymous";

                    return RateLimitPartition
                        .GetFixedWindowLimiter(
                            partitionKey: userId,
                            factory: _ =>
                                new FixedWindowRateLimiterOptions
                                {
                                    PermitLimit =
                                        payoutPermitLimit,

                                    Window =
                                        payoutWindow,

                                    QueueLimit = 0,

                                    AutoReplenishment = true
                                });
                });

        options.OnRejected =
            async (
                context,
                cancellationToken) =>
            {
                var retryAfter =
                    loginWindow;

                if (context.Lease.TryGetMetadata(
                        MetadataName.RetryAfter,
                        out var metadataRetryAfter))
                {
                    retryAfter =
                        metadataRetryAfter;
                }

                var retrySeconds =
                    Math.Max(
                        1,
                        (int)Math.Ceiling(
                            retryAfter.TotalSeconds));

                context.HttpContext
                    .Response
                    .Headers
                    .RetryAfter =
                        retrySeconds.ToString();

                var problemDetailsService =
                    context.HttpContext
                        .RequestServices
                        .GetRequiredService<
                            IProblemDetailsService>();

                var problem =
                    new ProblemDetails
                    {
                        Status =
                            StatusCodes
                                .Status429TooManyRequests,

                        Title =
                            "Too Many Requests",

                        Detail =
                            $"Too many attempts. Try again in {retrySeconds} seconds.",

                        Instance =
                            context.HttpContext
                                .Request.Path
                    };

                await problemDetailsService
                    .TryWriteAsync(
                        new ProblemDetailsContext
                        {
                            HttpContext =
                                context.HttpContext,

                            ProblemDetails =
                                problem
                        });
            };
    });

builder.Services.AddSingleton(
    TimeProvider.System);

builder.Services.AddScoped<
    IMemberHomeService,
    MemberHomeService>();

var app = builder.Build();

// Recreates the development data previously provided
// by InMemoryTrackStore when the database is empty.
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<RondiTrackDbContext>();

    await dbContext.Database.MigrateAsync();

    await DbSeeder.SeedAsync(dbContext);
}

app.UseExceptionHandler();

app.UseStatusCodePages(async context =>
{
    var response =
        context.HttpContext.Response;

    if (response.StatusCode !=
            StatusCodes.Status401Unauthorized &&
        response.StatusCode !=
            StatusCodes.Status403Forbidden)
    {
        return;
    }

    var problemDetailsService =
        context.HttpContext.RequestServices
            .GetRequiredService<
                IProblemDetailsService>();

    var title =
        response.StatusCode ==
        StatusCodes.Status401Unauthorized
            ? "Unauthorized"
            : "Forbidden";

    var detail =
        response.StatusCode ==
        StatusCodes.Status401Unauthorized
            ? "Authentication is required to access this resource."
            : "You do not have permission to access this resource.";

    await problemDetailsService.TryWriteAsync(
        new ProblemDetailsContext
        {
            HttpContext =
                context.HttpContext,

            ProblemDetails =
                new ProblemDetails
                {
                    Status =
                        response.StatusCode,

                    Title =
                        title,

                    Detail =
                        detail,

                    Instance =
                        context.HttpContext
                            .Request.Path
                }
        });
});

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi().AllowAnonymous();

    app.MapScalarApiReference(options =>
    {
        options.WithTitle("RondiTrack API");
    }).AllowAnonymous();
}

app.MapHealthChecks("/health/live", new HealthCheckOptions
    {
        Predicate = _ => false
    }).AllowAnonymous();

app.MapHealthChecks("/health/ready", new HealthCheckOptions
    {
        Predicate = check => check.Tags.Contains("ready")
    }).AllowAnonymous();

// app.UseHttpsRedirection();

app.UseHsts();
app.UseCors(CorsPolicies.Frontend);
app.UseAuthentication();
app.UseRateLimiter();
app.UseAuthorization();

app.MapControllers();

app.Run();

// To makeentry point for integration tests
public partial class Program { }