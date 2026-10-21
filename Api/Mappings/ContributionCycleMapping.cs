using Api.DTO;
using Domain.Models;

namespace Api.Mappings;

public static class ContributionCycleMappings
{
    public static ContributionCycle ToDomain(
        this CreateContributionCycleRequest request,
        Guid stokvelId)
    {
        return new ContributionCycle(
            stokvelId,
            request.CycleNumber,
            request.TargetAmount);
    }

    public static void ApplyTo(
        this UpdateContributionCycleRequest request,
        ContributionCycle cycle)
    {
        cycle.Update(
            request.CycleNumber,
            request.TargetAmount);
    }

    public static ContributionCycleResponse ToResponse(
        this ContributionCycle cycle,
        decimal totalCollected = 0m)
    {
        // A payout takes precedence over the closed timestamp.
        var status = cycle.Status == ContributionCycleStatus.PaidOut
            ? "PaidOut"
            : cycle.ClosedAtUtc.HasValue
                ? "Closed"
                : "Open";

        return new ContributionCycleResponse(
            cycle.Id,
            cycle.StokvelId,
            cycle.CycleNumber,
            cycle.TargetAmount,
            cycle.Version,
            status,
            cycle.EndDate,
            ToCents(totalCollected),
            ToCents(cycle.TargetAmount));
    }

    private static long ToCents(decimal amount)
    {
        return checked((long)decimal.Round(
            amount * 100m,
            0,
            MidpointRounding.AwayFromZero));
    }
}
