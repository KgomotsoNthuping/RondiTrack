using Domain.Models;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Api.Data;

public static class CycleSeeder
{
    public static async Task SeedAsync(RondiTrackDbContext db)
    {
        var stokvel = await db.Stokvels
            .SingleOrDefaultAsync(s =>
                s.Name == FrontendSeeder.FamilyName);

        var user = await db.Users
            .SingleOrDefaultAsync(u =>
                u.Email == FrontendSeeder.DemoEmail);

        if (stokvel is null || user is null)
        {
            return;
        }

        var existingCycles = await db.ContributionCycles
            .Where(c => c.StokvelId == stokvel.Id)
            .OrderBy(c => c.CycleNumber)
            .ToListAsync();

        if (existingCycles.Count >= 3)
        {
            return;
        }

        var nextCycleNumber = existingCycles.Count == 0
            ? 1
            : existingCycles.Max(c => c.CycleNumber) + 1;

        var targetAmount = existingCycles.Count > 0
            ? existingCycles[0].TargetAmount
            : stokvel.MonthlyContribution;

        var closedCycle = new ContributionCycle(
            stokvel.Id,
            nextCycleNumber,
            targetAmount,
            new DateOnly(2026, 9, 30));

        var paidOutCycle = new ContributionCycle(
            stokvel.Id,
            nextCycleNumber + 1,
            targetAmount,
            new DateOnly(2026, 8, 31));

        paidOutCycle.MarkPaidOut();

        db.ContributionCycles.AddRange(
            closedCycle,
            paidOutCycle);

        // Closed is represented by its closure timestamp.
        db.Entry(closedCycle)
            .Property(c => c.ClosedAtUtc)
            .CurrentValue = new DateTime(
                2026, 10, 1, 0, 0, 0,
                DateTimeKind.Utc);

        await db.SaveChangesAsync();

        var alreadyInArrears = await db.Arrears.AnyAsync(a =>
            a.StokvelId == stokvel.Id &&
            a.UserId == user.Id &&
            a.ContributionCycleId == closedCycle.Id);

        if (!alreadyInArrears)
        {
            db.Arrears.Add(new Arrear(
                stokvel.Id,
                user.Id,
                closedCycle.Id,
                15000,
                new DateTime(
                    2026, 10, 1, 0, 0, 0,
                    DateTimeKind.Utc)));

            await db.SaveChangesAsync();
        }
    }
}