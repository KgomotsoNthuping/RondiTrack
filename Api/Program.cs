using Api.Data;
using Scalar.AspNetCore;
using Api.Services;
using Api.Validation;
using Api.ErrorHandling;

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
builder.Services.AddSingleton<
    ITrackStore,
    InMemoryTrackStore>();

// Stores Idempotency-Key results in memory.
builder.Services.AddSingleton<
    IIdempotency,
    InMemoryIdempotency>();

// Contains RondiTrack business decisions.
builder.Services.AddSingleton<
    ITrackService,
    TrackService>()

var app = builder.Build();

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