using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Testcontainers.PostgreSql;
using Xunit;
using System.Security.Cryptography;
using Microsoft.Extensions.Logging;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using Api.Auth;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace Api.Tests.TestSupport;

public class PostgresApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private static readonly string TestSigningKey =
    Convert.ToBase64String(
        RandomNumberGenerator.GetBytes(48));

    public TestLogProvider Logs { get; } =
        new();
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:17")
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

    public HttpClient CreateAuthenticatedClient(
    Guid userId,
    string role = "User")
    {
        var client = CreateClient();

        var jwt = Services.GetRequiredService<JwtOptions>();

        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(jwt.SigningKey));

        var credentials = new SigningCredentials(
            key,
            SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim("sub", userId.ToString()),
            new Claim("role", role)
        };

        var token = new JwtSecurityToken(
            issuer: jwt.Issuer,
            audience: jwt.Audience,
            claims: claims,
            notBefore: DateTime.UtcNow.AddMinutes(-1),
            expires: DateTime.UtcNow.AddMinutes(30),
            signingCredentials: credentials);

        var tokenValue = new JwtSecurityTokenHandler()
            .WriteToken(token);

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                tokenValue);

        return client;
    }
    async Task IAsyncLifetime.DisposeAsync()
    {
        await _container.DisposeAsync();
    }

    [CollectionDefinition("Postgres collection")]
    public class PostgresCollection : ICollectionFixture<PostgresApiFactory>
    {
    }
}