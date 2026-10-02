using Api.Models;

namespace Api.Data;

//The operations of the application, keeps controllers from storage implementation
public interface ITrackStore
{
    //User
    Task<IReadOnlyCollection<User>> GetUsersAsync();

    Task<User?> GetUserByIdAsync(Guid id);

    Task AddUserAsync(User user);

    Task UpdateUserAsync(User user);

    Task<bool> DeleteUserAsync(Guid id);

    Task<bool> IsUserMemberOfAnyStokvelAsync(Guid userId);

        // Stokvel Membership
    Task<StokvelMember?> GetStokvelMemberAsync(Guid stokvelId, Guid userId);

    Task AddStokvelMemberAsync(StokvelMember member);

    Task<bool> DeleteStokvelMemberAsync(Guid stokvelId, Guid userId);

    //Stokvel
    Task<IReadOnlyCollection<Stokvel>> GetStokvelsAsync();

    Task<Stokvel?> GetStokvelByIdAsync(Guid id);

    Task AddStokvelAsync(Stokvel stokvel);

    Task UpdateStokvelAsync(Stokvel stokvel);

    Task<bool> DeleteStokvelAsync(Guid id);

    //Contribution
    Task<Contribution?> GetContributionByIdAsync(Guid stokvelId, Guid contributionId);

    Task<Contribution?> GetContributionAsync(Guid stokvelId, Guid userId, Guid contributionCycleId);

    Task AddContributionAsync(Contribution contribution);

    //ContributionCycle
    Task<IReadOnlyCollection<ContributionCycle>>
        GetContributionCyclesAsync(Guid stokvelId);

    Task<ContributionCycle?>
        GetContributionCycleByIdAsync(Guid stokvelId, Guid cycleId);

    Task AddContributionCycleAsync(ContributionCycle cycle);

    Task UpdateContributionCycleAsync(ContributionCycle cycle);

    Task<bool> DeleteContributionCycleAsync(Guid stokvelId, Guid cycleId);
}