using Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Api.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(RondiTrackDbContext dbContext)
    {
        if (await dbContext.Users.AnyAsync())
        {
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

        // Membership used to live only inside Stokvel.
        // It is now persisted through StokvelMember.
        await dbContext.StokvelMembers.AddRangeAsync(
            new StokvelMember(
                ubuntuSavers.Id,
                testOne.Id),

            new StokvelMember(
                ubuntuSavers.Id,
                testTwo.Id),

            new StokvelMember(
                communitySavers.Id,
                testThree.Id));

        var septemberCycle =
            new ContributionCycle(
                ubuntuSavers.Id,
                9,
                500.00m);

        await dbContext.ContributionCycles
            .AddAsync(septemberCycle);

        await dbContext.SaveChangesAsync();
    }
}