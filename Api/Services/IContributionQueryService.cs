using Api.DTO;

namespace Api.Services;

public interface IContributionQueryService
{
    Task<PagedResponse<CycleContributionResponse>> GetCycleContributionsAsync(
            Guid stokvelId,
            Guid cycleId,
            CycleContributionPageRequest request);
}