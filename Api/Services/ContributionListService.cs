
using System.Globalization;
using Api.DTO;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Api.Services;

public sealed class ContributionListService
{
    private readonly RondiTrackDbContext _dbContext;

    public ContributionListService(RondiTrackDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PagedResponse<ContributionListItemResponse>>GetAsync(Guid stokvelId, int pageSize, int offset)
    {
        var rows = await _dbContext.Contributions
            .AsNoTracking()
            .Where(c => c.StokvelId == stokvelId)
            .Include(c => c.ContributionCycle)
            .OrderBy(c => c.Id)
            .Skip(offset)
            .Take(pageSize + 1)
            .ToListAsync();

        var hasMore = rows.Count > pageSize;

        var items = rows.Take(pageSize)
            .Select(c => new ContributionListItemResponse(
                c.Id,
                c.ContributionCycleId,
                checked((long)decimal.Round(
                    c.Amount * 100m,
                    0,
                    MidpointRounding.AwayFromZero)),
                c.Status.ToString(),
                c.PaidAtUtc,
                c.ContributionCycle.CycleNumber))
            .ToList();

        var nextPageToken = hasMore ? (offset + pageSize).ToString(CultureInfo.InvariantCulture): string.Empty;

        return new PagedResponse<ContributionListItemResponse>(
            items,
            nextPageToken);
    }
}
