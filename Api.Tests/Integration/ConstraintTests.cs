using Api.Data;
using Api.Models;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Api.Tests.Integration;

public sealed class ConstraintTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public ConstraintTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task
        DuplicateContribution_IsRejectedByDatabase()
    {
        using var scope =
            _factory.Services.CreateScope();

        var dbContext =
            scope.ServiceProvider
                .GetRequiredService<
                    RondiTrackDbContext>();

        var stokvel =
            new Stokvel(
                $"Constraint Test {Guid.NewGuid()}",
                500m,
                1,
                1);

        var user =
            new User(
                "Constraint Test User",
                $"constraint-{Guid.NewGuid()}@test.local",
                "0712345678");

        dbContext.Stokvels.Add(stokvel);
        dbContext.Users.Add(user);

        await dbContext.SaveChangesAsync();

        var member =
            new StokvelMember(
                stokvel.Id,
                user.Id,
                StokvelMemberRole.Member);

        var cycle =
            new ContributionCycle(
                stokvel.Id,
                1,
                500m);

        dbContext.StokvelMembers.Add(member);
        dbContext.ContributionCycles.Add(cycle);

        await dbContext.SaveChangesAsync();

        var firstContribution =
            new Contribution(
                stokvel.Id,
                user.Id,
                cycle.Id,
                500m);

        dbContext.Contributions.Add(
            firstContribution);

        await dbContext.SaveChangesAsync();

        var duplicateContribution =
            new Contribution(
                stokvel.Id,
                user.Id,
                cycle.Id,
                500m);

        dbContext.Contributions.Add(
            duplicateContribution);

        await Assert.ThrowsAsync<
            DbUpdateException>(
                async () =>
                    await dbContext
                        .SaveChangesAsync());
    }
}