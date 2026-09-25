using Api.Models;

namespace Api.Services;

public interface ITrackService
{
    Task<User> AddMemberAsync(
        Guid stokvelId,
        Guid userId);

    Task RemoveMemberAsync(
        Guid stokvelId,
        Guid userId);

    Task DeleteUserAsync(
        Guid userId);

    Task<Contribution> RecordContributionAsync(
        Guid stokvelId,
        Guid userId,
        Guid contributionCycleId,
        decimal amount,
        string idempotencyKey);
}