using Api.Data;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddOpenApi();

// To use inmemory store instance 
builder.Services.AddSingleton<
    ITrackStore,
    InMemoryTrackStore>();

var app = builder.Build();

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