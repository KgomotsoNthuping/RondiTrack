
using System.Net.Http.Json;
using Api.Data;
using Api.DTO;
using Api.Tests.TestSupport;
using Domain.Models;
using Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Api.Tests.Integration;

[Collection("Postgres collection")]
public sealed class FrontendDemoSeedTests
{
    private readonly PostgresApiFactory _factory;

    public FrontendDemoSeedTests(
        PostgresApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Demo_has_required_roles_and_payments()
    {
        using var scope = _factory.Services.CreateScope();

        var db = scope.ServiceProvider
            .GetRequiredService<RondiTrackDbContext>();

        var lerato = await db.Users.SingleAsync(
            user =>
                user.Email == FrontendSeeder.DemoEmail);

        using var client =
            _factory.CreateAuthenticatedClient(lerato.Id);

        var home = await client.GetFromJsonAsync<
            MemberHomeResponse>("/api/v1/me/home");

        Assert.NotNull(home);
        Assert.Equal(2, home.Stokvels.Count);

        var family = home.Stokvels.Single(
            item =>
                item.Name == FrontendSeeder.FamilyName);

        var work = home.Stokvels.Single(
            item =>
                item.Name == FrontendSeeder.WorkName);

        Assert.Equal("Treasurer", family.Role);
        Assert.Equal("Member", work.Role);

        Assert.Equal(0L, family.StillOwedInCents);
        Assert.Equal(70000L, work.StillOwedInCents);

        var familyId = await db.Stokvels
            .Where(s =>
                s.Name == FrontendSeeder.FamilyName)
            .Select(s => s.Id)
            .SingleAsync();

        var contributions = await db.Contributions
            .Where(c => c.StokvelId == familyId)
            .ToListAsync();

        Assert.Contains(contributions,
            c => c.Status == ContributionStatus.Pending);

        Assert.Contains(contributions,
            c => c.Status == ContributionStatus.Paid);

        Assert.Contains(contributions,
            c => c.Status == ContributionStatus.Confirmed);

        var midnight = Assert.Single(
            contributions,
            c => c.Amount == 0.10m);

        Assert.Equal(
            new DateTime(
                2026, 10, 31, 22, 30, 0,
                DateTimeKind.Utc),
            midnight.PaidAtUtc);

        var hasher = scope.ServiceProvider
            .GetRequiredService<IPasswordHasher<User>>();

        // Running the seeder again creates no duplicates.
        await FrontendSeeder.SeedAsync(
            db, hasher, null);

        Assert.Equal(1, await db.Users.CountAsync(
            u => u.Email == FrontendSeeder.DemoEmail));
    }
}
