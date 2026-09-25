using Api.Models;

namespace Api.Services;

public interface ITrackService
{
    Task<ServiceResult<User>> AddMemberAsync(
        Guid stokvelId,
        Guid userId);

    Task<ServiceResult<bool>> RemoveMemberAsync(
        Guid stokvelId,
        Guid userId);

    Task<ServiceResult<bool>> DeleteUserAsync(
        Guid userId);

    Task<ServiceResult<Contribution>>
        RecordContributionAsync(
            Guid stokvelId,
            Guid userId,
            int cycleNumber,
            decimal amount,
            string? idempotencyKey);
}