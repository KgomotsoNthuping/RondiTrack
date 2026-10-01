using Api.Data;
using Api.Exceptions;
using Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Api.Services;

public sealed class PayoutService : IPayoutService
{
    private readonly RondiTrackDbContext _dbContext;

    public PayoutService(RondiTrackDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Payout> ProcessPayoutAsync(
        Guid stokvelId,
        Guid contributionCycleId)
    {
        
        var strategy = _dbContext.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await _dbContext.Database.BeginTransactionAsync();

                try
                {
                    var payout = await ProcessInsideTransactionAsync(
                            stokvelId,
                            contributionCycleId);

                    await transaction.CommitAsync();

                    return payout;
                }
                catch
                {
                    await transaction.RollbackAsync();

                    throw;
                }
            });
    }

    private async Task<Payout> ProcessInsideTransactionAsync(
            Guid stokvelId,
            Guid contributionCycleId)
    {
        var stokvelExists = await _dbContext.Stokvels.AnyAsync(stokvel => stokvel.Id == stokvelId);

        if (!stokvelExists)
        {
            throw new ResourceNotFoundException($"Stokvel '{stokvelId}' was not found.");
        }

        var cycle = await _dbContext.ContributionCycles.FirstOrDefaultAsync(cycle =>
                    cycle.Id == contributionCycleId &&
                    cycle.StokvelId == stokvelId);

        if (cycle is null)
        {
            throw new ResourceNotFoundException($"Contribution cycle '{contributionCycleId}' was not found.");
        }

        if (cycle.Status == ContributionCycleStatus.PaidOut)
        {
            throw new ConflictException("This contribution cycle has already been paid out.");
        }

        var members = await _dbContext.StokvelMembers
                .Where(member => member.StokvelId == stokvelId)
                .OrderBy(member => member.JoinedAtUtc)
                .ThenBy(member => member.Id)
                .ToListAsync();

        if (members.Count == 0)
        {
            throw new BusinessRuleException("A payout cannot be processed because the stokvel has no members.");
        }

        var previousPayoutCount = await _dbContext.Payouts
                .CountAsync(payout => payout.StokvelId == stokvelId);

        var nextMemberIndex = previousPayoutCount % members.Count;

        var recipient = members[nextMemberIndex];

        var payout = new Payout(
                stokvelId,
                contributionCycleId,
                recipient.Id,
                cycle.TargetAmount);

        await _dbContext.Payouts.AddAsync(payout);

        await _dbContext.SaveChangesAsync();

        cycle.MarkPaidOut();

        await _dbContext.SaveChangesAsync();

        return payout;
    }
}