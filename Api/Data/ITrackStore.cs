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
    public Task<IReadOnlyCollection<ContributionCycle>>
    GetContributionCyclesAsync(Guid stokvelId)
    {
        IReadOnlyCollection<ContributionCycle> cycles =
         _contributionCycles
            .Where(cycle => cycle.StokvelId == stokvelId)
            .ToList()
            .AsReadOnly();

        return Task.FromResult(cycles);
    }

    public Task<ContributionCycle?>
        GetContributionCycleByIdAsync(
            Guid stokvelId,
            Guid cycleId)
    {
        var cycle =
            _contributionCycles.FirstOrDefault(cycle =>
                cycle.Id == cycleId &&
                cycle.StokvelId == stokvelId);

        return Task.FromResult(cycle);
    }

    public Task AddContributionCycleAsync(
        ContributionCycle cycle)
    {
        _contributionCycles.Add(cycle);

        return Task.CompletedTask;
    }

    public Task UpdateContributionCycleAsync(
        ContributionCycle cycle)
    {
        var index =
            _contributionCycles.FindIndex(existing =>
                existing.Id == cycle.Id);

        if (index >= 0)
        {
            _contributionCycles[index] = cycle;
        }

        return Task.CompletedTask;
    }

    public Task<bool> DeleteContributionCycleAsync(
         Guid stokvelId,
        Guid cycleId)
    {
        var cycle =
            _contributionCycles.FirstOrDefault(cycle =>
                cycle.Id == cycleId &&
                cycle.StokvelId == stokvelId);

        if (cycle is null)
        {
            return Task.FromResult(false);
        }

        _contributionCycles.Remove(cycle);

        return Task.FromResult(true);
    }
    
}