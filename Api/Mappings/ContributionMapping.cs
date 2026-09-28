using Api.DTO;
using Api.Models;

namespace Api.Mappings;

public static class ContributionMapping
{
    public static ContributionResponse ToResponse(this Contribution contribution)
    {
        return new ContributionResponse(
            contribution.Id,
            contribution.StokvelId,
            contribution.UserId,
            contribution.ContributionCycleId,
            contribution.Amount);
    }
}