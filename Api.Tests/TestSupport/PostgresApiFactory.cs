using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Testcontainers.PostgreSql;
using Xunit;
using System.Security.Cryptography;
using Microsoft.Extensions.Logging;

namespace Api.Tests.TestSupport;

public class PostgresApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private static readonly string TestSigningKey =
    Convert.ToBase64String(
        RandomNumberGenerator.GetBytes(48));

    public TestLogProvider Logs { get; } =
        new();
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder()
            .WithImage("postgres:17")
            .WithDatabase("ronditrack_test")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .Build();

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting(
            "ConnectionStrings:RondiTrack",
            _container.GetConnectionString());

        builder.UseSetting(
            "Jwt:SigningKey",
            TestSigningKey);

        builder.ConfigureLogging(logging =>
        {
            logging.AddProvider(Logs);
        });

        builder.UseSetting(
            "Cors:AllowedOrigins:0",
            "http://localhost:3000");

        builder.UseSetting(
            "RateLimiting:Login:PermitLimit",
            "1000");

        builder.UseSetting(
            "RateLimiting:Contribution:PermitLimit",
            "1000");

        builder.UseSetting(
            "RateLimiting:Payout:PermitLimit",
            "1000");
    }
    async Task IAsyncLifetime.DisposeAsync()
    {
        await _container.DisposeAsync();
    }
}

[CollectionDefinition("Postgres collection")]
public class PostgresCollection : ICollectionFixture<PostgresApiFactory>
{
}
