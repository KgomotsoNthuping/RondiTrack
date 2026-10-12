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

var builder = WebApplication.CreateBuilder(args);

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
    options.SuppressModelStateInvalidFilter = true;
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
            context.HttpContext.TraceIdentifier;

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

builder.Services.AddAuthorization();

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

                ValidAlgorithms = [ SecurityAlgorithms.HmacSha256 ]
            };
    });


    
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

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    app.MapScalarApiReference(options =>
    {
        options.WithTitle("RondiTrack API");
    });
}

// app.UseHttpsRedirection();

app.UseHsts();

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

app.Run();

// To makeentry point for integration tests
public partial class Program { }