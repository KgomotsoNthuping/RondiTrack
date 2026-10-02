using Api.Data;
using Scalar.AspNetCore;
using FluentValidation;
using Api.Services;
using Api.Validation;
using Api.ErrorHandling;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

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

var app = builder.Build();

// Recreates the development data previously provided
// by InMemoryTrackStore when the database is empty.
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<RondiTrackDbContext>();

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

app.MapControllers();

app.Run();

// To makeentry point for integration tests
public partial class Program { }