using Api.Models;

namespace Api.Services;

public interface IPayoutService
{
    Task<Payout> ProcessPayoutAsync(
        Guid stokvelId,
        Guid contributionCycleId);
}