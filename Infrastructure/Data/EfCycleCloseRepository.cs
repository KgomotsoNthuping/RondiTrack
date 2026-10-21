using Domain.Models;
using Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Data;

public sealed class EfCycleCloseRepository : ICycleCloseRepository
{
    private readonly RondiTrackDbContext _dbContext;

    public EfCycleCloseRepository(RondiTrackDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<bool> TryCloseCycleAsync(Guid cycleId,DateTime closedAtUtc, CancellationToken cancellationToken = default)
    {
        if (closedAtUtc.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException("The cycle close time must be UTC.", nameof(closedAtUtc));
        }

        var affectedRows = await _dbContext.ContributionCycles
                .Where(cycle =>cycle.Id == cycleId && cycle.ClosedAtUtc == null)
                .ExecuteUpdateAsync(setters =>
                        setters.SetProperty(cycle => cycle.ClosedAtUtc, closedAtUtc),
                    cancellationToken);

        return affectedRows == 1;
    }
}