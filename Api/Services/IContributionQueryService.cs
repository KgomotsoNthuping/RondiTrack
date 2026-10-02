using Api.DTO;

namespace Api.Services;

public interface IContributionQueryService
{
    Task<IReadOnlyCollection<CycleContributionResponse>> GetCycleContributionsAsync(
            Guid stokvelId,
            Guid cycleId);
}