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

        
        var contributions = await _dbContext.Contributions
            .AsNoTracking()
            .Where(contribution =>
                contribution.StokvelId == stokvelId &&
                contribution.ContributionCycleId == cycleId)
            .Include(contribution => contribution.Member)
            .ThenInclude(member => member.User)
            .ToListAsync();

        var response = contributions.Select(contribution =>
                new CycleContributionResponse(
                    contribution.Id,
                    contribution.UserId,
                    contribution.Member.User.FullName,
                    contribution.Member.Role,
                    contribution.Amount))
            .ToList();

        return response;
    }
}