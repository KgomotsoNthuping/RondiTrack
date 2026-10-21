
using Domain.Models;
using Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Api.Data;

public static class FrontendSeeder
{
    public const string DemoEmail =
        "lerato@ronditrack.test";

    public const string FamilyName =
        "Lerato Family Savers (Demo)";

    public const string WorkName =
        "Lerato Work Savers (Demo)";

    public static async Task SeedAsync(
    RondiTrackDbContext db,
    IPasswordHasher<User> passwordHasher,
    string? demoPassword,
    bool resetDemoPasswords = false)
    {
        // Existing demo users: optionally reset their passwords
// in Development when explicitly enabled.
if (await db.Users.AnyAsync(
        user => user.Email == DemoEmail))
{
    if (resetDemoPasswords &&
        !string.IsNullOrWhiteSpace(demoPassword))
    {
        var demoEmails = new[]
        {
            DemoEmail,
            "nandi.demo@ronditrack.test",
            "thabo.demo@ronditrack.test"
        };

        var existingUsers = await db.Users
            .Where(user => demoEmails.Contains(user.Email))
            .ToListAsync();

        foreach (var user in existingUsers)
        {
            user.SetPasswordHash(
                passwordHasher.HashPassword(
                    user,
                    demoPassword));
        }

        await db.SaveChangesAsync();
    }

    return;
}
        var lerato = new User(
            "Lerato Demo",
            DemoEmail,
            "0820000001");

        var nandi = new User(
            "Nandi Demo",
            "nandi.demo@ronditrack.test",
            "0820000002");

        var thabo = new User(
            "Thabo Demo",
            "thabo.demo@ronditrack.test",
            "0820000003");

        if (!string.IsNullOrWhiteSpace(demoPassword))
        {
            foreach (var user in new[]
                     {
                         lerato, nandi, thabo
                     })
            {
                user.SetPasswordHash(
                    passwordHasher.HashPassword(
                        user,
                        demoPassword));
            }
        }

        var family = new Stokvel(
            FamilyName,
            500m,
            1,
            12);

        var work = new Stokvel(
            WorkName,
            1000m,
            1,
            12);

        var familyCycle = new ContributionCycle(
            family.Id,
            1,
            1500m,
            new DateOnly(2026, 10, 31));

        var workCycle = new ContributionCycle(
            work.Id,
            1,
            1000m,
            new DateOnly(2026, 11, 30));

        db.Users.AddRange(
            lerato, nandi, thabo);

        db.Stokvels.AddRange(
            family, work);

        db.StokvelMembers.AddRange(
            new StokvelMember(
                family.Id,
                lerato.Id,
                StokvelMemberRole.Treasurer),

            new StokvelMember(
                family.Id,
                nandi.Id,
                StokvelMemberRole.Member),

            new StokvelMember(
                family.Id,
                thabo.Id,
                StokvelMemberRole.Member),

            new StokvelMember(
                work.Id,
                lerato.Id,
                StokvelMemberRole.Member));

        db.ContributionCycles.AddRange(
            familyCycle,
            workCycle);

        db.Contributions.AddRange(
            // Lerato has fully paid Family Savers.
            new Contribution(
                family.Id,
                lerato.Id,
                familyCycle.Id,
                500m,
                ContributionStatus.Confirmed,
                new DateTime(
                    2026, 10, 15, 12, 0, 0,
                    DateTimeKind.Utc)),

            // Ten cents at 00:30 Johannesburg time.
            new Contribution(
                family.Id,
                nandi.Id,
                familyCycle.Id,
                0.10m,
                ContributionStatus.Paid,
                new DateTime(
                    2026, 10, 31, 22, 30, 0,
                    DateTimeKind.Utc)),

            // Pending contributions have no paid date.
            new Contribution(
                family.Id,
                thabo.Id,
                familyCycle.Id,
                200m,
                ContributionStatus.Pending),

            // Lerato still owes R700 in Work Savers.
            new Contribution(
                work.Id,
                lerato.Id,
                workCycle.Id,
                300m,
                ContributionStatus.Paid,
                new DateTime(
                    2026, 10, 18, 10, 0, 0,
                    DateTimeKind.Utc)));

        await db.SaveChangesAsync();
    }
}
