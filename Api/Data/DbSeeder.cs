using Domain.Models;
using Microsoft.EntityFrameworkCore;
using Infrastructure.Data;
using Microsoft.AspNetCore.Identity;

namespace Api.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(
        RondiTrackDbContext dbContext,
        IPasswordHasher<User> passwordHasher,
        string? demoPassword)
    {
        
        if (await dbContext.Users.AnyAsync())
        {
            await FrontendSeeder.SeedAsync(
                dbContext,
                passwordHasher,
                demoPassword);

            return;
        }


        var testOne = new User(
            "Test One",
            "testone@ronditrack.co.za",
            "0711111111");

        var testTwo = new User(
            "Test Two",
            "testtwo@ronditrack.co.za",
            "0722222222");

        var testThree = new User(
            "Kgomotso Nthuping",
            "kgomotso@ronditrack.co.za",
            "0733333333");

        if (!string.IsNullOrWhiteSpace(
                demoPassword))
        {
            testOne.SetPasswordHash(
                passwordHasher.HashPassword(
                    testOne,
                    demoPassword));

            testTwo.SetPasswordHash(
                passwordHasher.HashPassword(
                    testTwo,
                    demoPassword));

            testThree.SetPasswordHash(
                passwordHasher.HashPassword(
                    testThree,
                    demoPassword));
        }

        await dbContext.Users.AddRangeAsync(
            testOne,
            testTwo,
            testThree);

        var ubuntuSavers = new Stokvel(
            "Ubuntu Savers",
            500.00m,
            9,
            12,
            null);

        var communitySavers = new Stokvel(
            "Community Builders",
            1000.00m,
            3,
            12,
            "Monthly contributions must be paid before the 5th.");

        await dbContext.Stokvels.AddRangeAsync(
            ubuntuSavers,
            communitySavers);

        await dbContext.StokvelMembers.AddRangeAsync(
            new StokvelMember(
                ubuntuSavers.Id,
                testOne.Id,
                StokvelMemberRole.Admin),

            new StokvelMember(
                ubuntuSavers.Id,
                testTwo.Id,
                StokvelMemberRole.Treasurer),

            new StokvelMember(
                communitySavers.Id,
                testThree.Id,
                StokvelMemberRole.Member),

            new StokvelMember(
                communitySavers.Id,
                testOne.Id,
                StokvelMemberRole.Member));

        var septemberCycle =
            new ContributionCycle(
                ubuntuSavers.Id,
                9,
                500.00m);

        await dbContext.ContributionCycles
            .AddAsync(septemberCycle);

        await dbContext.SaveChangesAsync();

        
        await FrontendSeeder.SeedAsync(
            dbContext,
            passwordHasher,
            demoPassword);

    }
}