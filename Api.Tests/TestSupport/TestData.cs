using Domain.Models;
using Infrastructure.Data;
using Microsoft.Extensions.DependencyInjection;

namespace Api.Tests.TestSupport;

public static class TestData
{
    public static async Task<TestScenario> CreateAsync(
        IServiceProvider services,
        int memberCount = 5,
        bool createPayout = false,
        bool createContributions = true)
    {
        using var scope = services.CreateScope();

        var dbContext =
            scope.ServiceProvider
                .GetRequiredService<RondiTrackDbContext>();

        var unique =
            Guid.NewGuid()
                .ToString("N");

        var stokvel =
            new Stokvel(
                $"Test Stokvel {unique}",
                500m,
                1,
                12);

        var cycle =
            new ContributionCycle(
                stokvel.Id,
                1,
                5000m);

        var users =
            new List<User>();

        var memberships =
            new List<StokvelMember>();

        var contributions =
            new List<Contribution>();

        for (var index = 0;
             index < memberCount;
             index++)
        {
            var user =
                new User(
                    $"Test Member {index + 1} {unique}",
                    $"member{index + 1}_{unique}@ronditrack.test",
                    $"082{index:D7}");

            users.Add(user);

            var role =
                index == 0
                    ? StokvelMemberRole.Admin
                    : StokvelMemberRole.Member;

            memberships.Add(
                new StokvelMember(
                    stokvel.Id,
                    user.Id,
                    role));

            if (createContributions)
            {
                contributions.Add(
                    new Contribution(
                        stokvel.Id,
                        user.Id,
                        cycle.Id,
                        500m + index));
            }
        }

        await dbContext.Stokvels
            .AddAsync(stokvel);

        await dbContext.Users
            .AddRangeAsync(users);

        await dbContext.ContributionCycles
            .AddAsync(cycle);

        await dbContext.StokvelMembers
            .AddRangeAsync(memberships);

        if (createContributions)
        {
            await dbContext.Contributions
                .AddRangeAsync(contributions);
        }

        Payout? payout = null;

        if (createPayout)
        {
            payout =
                new Payout(
                    stokvel.Id,
                    cycle.Id,
                    users[0].Id,
                    cycle.TargetAmount);

            await dbContext.Payouts
                .AddAsync(payout);
        }

        await dbContext.SaveChangesAsync();

        return new TestScenario(
            stokvel.Id,
            cycle.Id,
            users
                .Select(user => user.Id)
                .ToList(),
            payout?.Id ?? Guid.Empty);
    }
}

public sealed record TestScenario(
    Guid StokvelId,
    Guid CycleId,
    IReadOnlyCollection<Guid> UserIds,
    Guid PayoutId);
