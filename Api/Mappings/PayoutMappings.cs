using Api.DTO;
using Api.Models;

namespace Api.Mappings;

public static class PayoutMappings
{
    public static PayoutResponse ToResponse(this Payout payout)
    {
        return new PayoutResponse(
            payout.Id,
            payout.StokvelId,
            payout.ContributionCycleId,
            payout.RecipientMemberId,
            payout.Amount,
            payout.PaidAtUtc);
    }
}