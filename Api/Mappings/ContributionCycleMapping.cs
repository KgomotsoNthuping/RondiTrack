using Api.DTO;
using Api.Models;

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
        this ContributionCycle cycle)
    {
        return new ContributionCycleResponse(
            cycle.Id,
            cycle.StokvelId,
            cycle.CycleNumber,
            cycle.TargetAmount);
    }
}