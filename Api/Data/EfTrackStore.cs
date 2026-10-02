using Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Api.Data;

public sealed class EfTrackStore : ITrackStore
{
    private readonly RondiTrackDbContext _dbContext;

    public EfTrackStore(RondiTrackDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    // Users
    public async Task<IReadOnlyCollection<User>> GetUsersAsync()
    {
        return await _dbContext.Users.ToListAsync();
    }

    public async Task<User?> GetUserByIdAsync(Guid id)
    {
        return await _dbContext.Users.FirstOrDefaultAsync(user => user.Id == id);
    }

    public async Task AddUserAsync(User user)
    {
        await _dbContext.Users.AddAsync(user);

        await _dbContext.SaveChangesAsync();
    }

    public async Task UpdateUserAsync(User user)
    {
        _dbContext.Users.Update(user);

        await _dbContext.SaveChangesAsync();
    }

    public async Task<bool> DeleteUserAsync(Guid id)
    {
        var user = await _dbContext.Users.FirstOrDefaultAsync(user => user.Id == id);

        if (user is null)
        {
            return false;
        }

        _dbContext.Users.Remove(user);

        await _dbContext.SaveChangesAsync();

        return true;
    }

    public async Task<bool> IsUserMemberOfAnyStokvelAsync(Guid userId)
    {
        return await _dbContext.StokvelMembers.AnyAsync(member => member.UserId == userId);
    }

    // Stokvels
    public async Task<IReadOnlyCollection<Stokvel>> GetStokvelsAsync()
    {
        var stokvels = await _dbContext.Stokvels.ToListAsync();

        await LoadMembersAsync(stokvels);

        return stokvels;
    }

    public async Task<Stokvel?> GetStokvelByIdAsync(Guid id)
    {
        var stokvel = await _dbContext.Stokvels.FirstOrDefaultAsync(stokvel => stokvel.Id == id);

        if (stokvel is null)
        {
            return null;
        }

        await LoadMembersAsync([stokvel]);

        return stokvel;
    }

    public async Task AddStokvelAsync(Stokvel stokvel)
    {
        await _dbContext.Stokvels.AddAsync(stokvel);

        await _dbContext.SaveChangesAsync();
    }

    public async Task UpdateStokvelAsync(Stokvel stokvel)
    {
        await SynchronizeMembershipsAsync(stokvel);

        _dbContext.Stokvels.Update(stokvel);

        await _dbContext.SaveChangesAsync();
    }

    public async Task<bool> DeleteStokvelAsync(Guid id)
    {
        var stokvel = await _dbContext.Stokvels.FirstOrDefaultAsync(stokvel => stokvel.Id == id);

        if (stokvel is null)
        {
            return false;
        }

        var memberships = await _dbContext.StokvelMembers
                .Where(member => member.StokvelId == id)
                .ToListAsync();

        _dbContext.StokvelMembers.RemoveRange(memberships);

        _dbContext.Stokvels.Remove(stokvel);

        await _dbContext.SaveChangesAsync();

        return true;
    }

    // Contributions
    public async Task<Contribution?> GetContributionByIdAsync(
            Guid stokvelId,
            Guid contributionId)
    {
        return await _dbContext.Contributions
            .FirstOrDefaultAsync(contribution =>
                contribution.Id == contributionId &&
                contribution.StokvelId == stokvelId);
    }

    public async Task<Contribution?>
        GetContributionAsync(
            Guid stokvelId,
            Guid userId,
            Guid contributionCycleId)
    {
        return await _dbContext.Contributions
            .FirstOrDefaultAsync(contribution =>
                contribution.StokvelId == stokvelId &&
                contribution.UserId == userId &&
                contribution.ContributionCycleId ==
                    contributionCycleId);
    }

    public async Task AddContributionAsync(
        Contribution contribution)
    {
        await _dbContext.Contributions
            .AddAsync(contribution);

        await _dbContext.SaveChangesAsync();
    }

    // Contribution Cycles
    public async Task<IReadOnlyCollection<ContributionCycle>>
        GetContributionCyclesAsync(Guid stokvelId)
    {
        return await _dbContext.ContributionCycles
            .Where(cycle => cycle.StokvelId == stokvelId)
            .ToListAsync();
    }

    public async Task<ContributionCycle?>
        GetContributionCycleByIdAsync(
            Guid stokvelId,
            Guid cycleId)
    {
        return await _dbContext.ContributionCycles
            .FirstOrDefaultAsync(cycle =>
                cycle.Id == cycleId &&
                cycle.StokvelId == stokvelId);
    }

    public async Task AddContributionCycleAsync(
        ContributionCycle cycle)
    {
        await _dbContext.ContributionCycles
            .AddAsync(cycle);

        await _dbContext.SaveChangesAsync();
    }

    public async Task UpdateContributionCycleAsync(
        ContributionCycle cycle)
    {
        _dbContext.ContributionCycles.Update(cycle);

        await _dbContext.SaveChangesAsync();
    }

    public async Task<bool>
        DeleteContributionCycleAsync(
            Guid stokvelId,
            Guid cycleId)
    {
        var cycle =
            await _dbContext.ContributionCycles
                .FirstOrDefaultAsync(cycle =>
                    cycle.Id == cycleId &&
                    cycle.StokvelId == stokvelId);

        if (cycle is null)
        {
            return false;
        }

        _dbContext.ContributionCycles.Remove(cycle);

        await _dbContext.SaveChangesAsync();

        return true;
    }

    private async Task LoadMembersAsync(
        IEnumerable<Stokvel> stokvels)
    {
        var stokvelList = stokvels.ToList();

        if (stokvelList.Count == 0)
        {
            return;
        }

        var stokvelIds = stokvelList
            .Select(stokvel => stokvel.Id)
            .ToList();

        var memberships =
            await _dbContext.StokvelMembers
                .Where(member =>
                    stokvelIds.Contains(
                        member.StokvelId))
                .ToListAsync();

        if (memberships.Count == 0)
        {
            return;
        }

        var userIds = memberships
            .Select(member => member.UserId)
            .Distinct()
            .ToList();

        var users = await _dbContext.Users
            .Where(user =>
                userIds.Contains(user.Id))
            .ToDictionaryAsync(user => user.Id);

        foreach (var stokvel in stokvelList)
        {
            var stokvelMemberships =
                memberships.Where(member =>
                    member.StokvelId ==
                    stokvel.Id);

            foreach (var membership
                     in stokvelMemberships)
            {
                if (!users.TryGetValue(
                        membership.UserId,
                        out var user))
                {
                    continue;
                }

                if (!stokvel.HasMember(user.Id))
                {
                    stokvel.AddMember(user);
                }
            }
        }
    }

    private async Task SynchronizeMembershipsAsync(
        Stokvel stokvel)
    {
        var existingMemberships =
            await _dbContext.StokvelMembers
                .Where(member =>
                    member.StokvelId ==
                    stokvel.Id)
                .ToListAsync();

        var currentMemberIds = stokvel.Members
            .Select(member => member.Id)
            .ToHashSet();

        foreach (var userId in currentMemberIds)
        {
            var alreadyPersisted =
                existingMemberships.Any(member =>
                    member.UserId == userId);

            if (!alreadyPersisted)
            {
                await _dbContext.StokvelMembers.AddAsync(
                    new StokvelMember(
                        stokvel.Id,
                        userId,
                        StokvelMemberRole.Member));
            }
        }

        var removedMemberships =
            existingMemberships
                .Where(member =>
                    !currentMemberIds.Contains(
                        member.UserId))
                .ToList();

         _dbContext.StokvelMembers
            .RemoveRange(removedMemberships);
    }
}