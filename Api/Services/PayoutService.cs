using Api.Data;
using Domain.Exceptions;
using Domain.Models;
using Microsoft.EntityFrameworkCore;
using Infrastructure.Data;
using System.Diagnostics;
using Api.Observability;

namespace Api.Services;

public sealed class PayoutService : IPayoutService
{
    private readonly RondiTrackDbContext _dbContext;

    private readonly IPayoutTransaction _transaction;

    public PayoutService(RondiTrackDbContext dbContext, IPayoutTransaction transaction)
    {
        _dbContext = dbContext;
        _transaction = transaction;
    }

    public async Task<Payout> ProcessPayoutAsync(Guid stokvelId, Guid contributionCycleId)
    {
        using var activity = Telemetry.ActivitySource.StartActivity("payout.process", ActivityKind.Internal);

        activity?.SetTag("payout.outcome", "started");

        try
        {
            var strategy = _dbContext.Database.CreateExecutionStrategy();

            var payout = await strategy.ExecuteAsync(async () =>
                    {
                        await using var transaction = await _dbContext.Database.BeginTransactionAsync();

                        try
                        {
                            var result = await ProcessInsideTransactionAsync(
                                    stokvelId,
                                    contributionCycleId,
                                    activity);

                            await transaction.CommitAsync();

                            return result;
                        }
                        catch
                        {
                            await transaction.RollbackAsync();

                            throw;
                        }
                    });

            activity?.SetTag("payout.outcome", "success");

            activity?.SetStatus(ActivityStatusCode.Ok);

            return payout;
        }
        catch
        {
            activity?.SetTag("payout.outcome", "failure");

            activity?.SetStatus(ActivityStatusCode.Error);

            throw;
        }
    }

    private async Task<Payout> ProcessInsideTransactionAsync(Guid stokvelId, Guid contributionCycleId, Activity? activity)
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

        activity?.SetTag("payout.cycle_number", cycle.CycleNumber);

        if (cycle.Status == ContributionCycleStatus.PaidOut)
        {
            throw new ConflictException("This contribution cycle has already been paid out.");
        }

        var members = await _dbContext.StokvelMembers
                .Where(member => member.StokvelId == stokvelId)
                .OrderBy(member => member.JoinedAtUtc)
                .ThenBy(member => member.UserId)
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
                recipient.UserId,
                cycle.TargetAmount);

        await _dbContext.Payouts.AddAsync(payout);

        await _dbContext.SaveChangesAsync();

        await _transaction.AfterPayoutSavedAsync();

        cycle.MarkPaidOut();

        await _dbContext.SaveChangesAsync();

        return payout;
    }
}