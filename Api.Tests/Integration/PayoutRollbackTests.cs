using Api.Data;
using Api.Models;
using Api.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Api.Tests.Integration;

public sealed class PayoutRollbackTests : IClassFixture<PayoutRollbackTests.RollbackWebApplicationFactory>
{
    private readonly RollbackWebApplicationFactory _factory;

    public PayoutRollbackTests(RollbackWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task ProcessPayout_WhenFailureOccursAfterPayoutInsert_RollsBackEverything()
    {
        Guid stokvelId;
        Guid cycleId;
        Guid userId;

        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider
                    .GetRequiredService<RondiTrackDbContext>();

            var user = new User(
                "Rollback Test User",
                $"rollback-{Guid.NewGuid()}@ronditrack.test",
                "0712345678");

            var stokvel = new Stokvel(
                $"Rollback Test {Guid.NewGuid()}",
                500.00m,
                1,
                12,
                null);

            var cycle = new ContributionCycle(
                stokvel.Id,
                1,
                500.00m);

            var member = new StokvelMember(
                stokvel.Id,
                user.Id);

            await dbContext.Users.AddAsync(user);
            await dbContext.Stokvels.AddAsync(stokvel);
            await dbContext.StokvelMembers.AddAsync(member);
            await dbContext.ContributionCycles.AddAsync(cycle);

            await dbContext.SaveChangesAsync();

            userId = user.Id;
            stokvelId = stokvel.Id;
            cycleId = cycle.Id;
        }

        try
        {
            using (var scope = _factory.Services.CreateScope())
            {
                var payoutService = scope.ServiceProvider
                        .GetRequiredService<IPayoutService>();

                await Assert.ThrowsAsync<InvalidOperationException>(() => payoutService.ProcessPayoutAsync(
                            stokvelId,
                            cycleId));
            }

            using (var verificationScope =  _factory.Services.CreateScope())
            {
                var dbContext = verificationScope.ServiceProvider
                        .GetRequiredService<RondiTrackDbContext>();

                var payoutExists = await dbContext.Payouts
                        .AsNoTracking()
                        .AnyAsync(payout => payout.ContributionCycleId == cycleId);

                var cycle = await dbContext.ContributionCycles
                        .AsNoTracking()
                        .SingleAsync(cycle => cycle.Id == cycleId);

                Assert.False(payoutExists);

                Assert.Equal(ContributionCycleStatus.Open, cycle.Status);
            }
        }
        finally
        {
            using var cleanupScope = _factory.Services.CreateScope();

            var dbContext = cleanupScope.ServiceProvider.GetRequiredService<RondiTrackDbContext>();

            var memberships = await dbContext.StokvelMembers
                    .Where(member => member.StokvelId == stokvelId)
                    .ToListAsync();

            var cycles = await dbContext.ContributionCycles
                    .Where(cycle => cycle.StokvelId == stokvelId)
                    .ToListAsync();

            var payouts = await dbContext.Payouts
                    .Where(payout => payout.StokvelId == stokvelId)
                    .ToListAsync();

            var stokvel = await dbContext.Stokvels
                    .FirstOrDefaultAsync(stokvel => stokvel.Id == stokvelId);

            var user = await dbContext.Users
                    .FirstOrDefaultAsync(user => user.Id == userId);

            dbContext.Payouts.RemoveRange(payouts);
            dbContext.StokvelMembers.RemoveRange(memberships);
            dbContext.ContributionCycles.RemoveRange(cycles);

            if (stokvel is not null)
            {
                dbContext.Stokvels.Remove(stokvel);
            }

            if (user is not null)
            {
                dbContext.Users.Remove(user);
            }

            await dbContext.SaveChangesAsync();
        }
    }

    public sealed class RollbackWebApplicationFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IPayoutTransaction>();

                services.AddScoped<
                    IPayoutTransaction,
                    ThrowAfterPayoutSave>();
            });
        }
    }

    private sealed class ThrowAfterPayoutSave : IPayoutTransaction
    {
        public Task AfterPayoutSavedAsync()
        {
            throw new InvalidOperationException("Forced failure for payout rollback test.");
        }
    }
}