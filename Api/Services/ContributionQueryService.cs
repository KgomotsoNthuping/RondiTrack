using Api.Data;
using Api.DTO;
using Api.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace Api.Services;

public sealed class ContributionQueryService : IContributionQueryService
{
    private readonly RondiTrackDbContext _dbContext;

    public ContributionQueryService(RondiTrackDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<
        IReadOnlyCollection<CycleContributionResponse>>
        GetCycleContributionsAsync(
            Guid stokvelId,
            Guid cycleId)
    {
        var cycleExists =
            await _dbContext.ContributionCycles
                .AsNoTracking()
                .AnyAsync(cycle =>
                    cycle.Id == cycleId &&
                    cycle.StokvelId == stokvelId);

        if (!cycleExists)
        {
            throw new ResourceNotFoundException(
                "The contribution cycle was not found.");
        }

        /*
         * DELIBERATELY NAIVE VERSION FOR ASSIGNMENT 5.2.
         *
         * First query:
         * load all Contribution rows for the cycle.
         */
        var contributions =
            await _dbContext.Contributions
                .AsNoTracking()
                .Where(contribution =>
                    contribution.StokvelId == stokvelId &&
                    contribution.ContributionCycleId == cycleId)
                .ToListAsync();

        var response =
            new List<CycleContributionResponse>();

        /*
         * N+1 problem:
         *
         * One additional database query is executed
         * for every Contribution.
         */
        foreach (var contribution in contributions)
        {
            var member =
                await _dbContext.StokvelMembers
                    .AsNoTracking()
                    .Include(member => member.User)
                    .SingleAsync(member =>
                        member.StokvelId ==
                            contribution.StokvelId &&
                        member.UserId ==
                            contribution.UserId);

            response.Add(
                new CycleContributionResponse(
                    contribution.Id,
                    contribution.UserId,
                    member.User.FullName,
                    member.Role,
                    contribution.Amount));
        }

        return response;
    }
}