using Api.DTO;
using Domain.Models;
using System.Globalization;

namespace Api.Mappings;

public static class ContributionMapping
{
    public static ContributionResponse ToResponse(
        this Contribution contribution)
    {
        var normalizedAmount = decimal.Parse(
            contribution.Amount.ToString(
                "G29",
                CultureInfo.InvariantCulture),
            CultureInfo.InvariantCulture);

        return new ContributionResponse(
            contribution.Id,
            contribution.StokvelId,
            contribution.UserId,
            contribution.ContributionCycleId,
            normalizedAmount);
    }
}