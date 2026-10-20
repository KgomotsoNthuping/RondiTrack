using Api.DTO;
using Domain.Models;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Api.Services;

public sealed class MemberHomeService
    : IMemberHomeService
{
    private readonly RondiTrackDbContext
        _dbContext;

    private readonly TimeProvider
        _timeProvider;

    public MemberHomeService(
        RondiTrackDbContext dbContext,
        TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _timeProvider = timeProvider;
    }

    public async Task<MemberHomeResponse>
        GetAsync(
            Guid userId)
    {
        var stokvels =
            await (
                from membership
                    in _dbContext
                        .StokvelMembers
                        .AsNoTracking()

                join stokvel
                    in _dbContext
                        .Stokvels
                        .AsNoTracking()
                    on membership.StokvelId
                    equals stokvel.Id

                join cycle
                    in _dbContext
                        .ContributionCycles
                        .AsNoTracking()
                    on new
                    {
                        StokvelId =
                            stokvel.Id,

                        CycleNumber =
                            stokvel.CurrentPeriod
                    }
                    equals new
                    {
                        cycle.StokvelId,
                        cycle.CycleNumber
                    }
                    into cycleGroup

                from cycle
                    in cycleGroup.DefaultIfEmpty()

                where membership.UserId ==
                    userId

                orderby stokvel.Name

                select new
                {
                    stokvel.Name,

                    membership.Role,

                    StokvelId = stokvel.Id,

                    CurrentCycle =
                        stokvel.CurrentPeriod,

                    MonthlyContribution =
                        stokvel.MonthlyContribution,

                    CycleDueDate =
                        cycle == null
                            ? (DateOnly?)null
                            : cycle.EndDate,

                    CycleClosedAtUtc =
                        cycle == null
                            ? (DateTime?)null
                            : cycle.ClosedAtUtc,

                    CycleId =
                        cycle == null
                            ? (Guid?)null
                            : cycle.Id,

                    CycleStatus =
                        cycle == null
                            ? (ContributionCycleStatus?)null
                            : cycle.Status
                }
            )
            .ToListAsync();

        var cycleIds =
            stokvels
                .Where(item =>
                    item.CycleId.HasValue)
                .Select(item =>
                    item.CycleId!.Value)
                .ToList();

        var payments =
            await _dbContext
                .Contributions
                .AsNoTracking()
                .Where(contribution =>
                    contribution.UserId ==
                        userId &&
                    cycleIds.Contains(
                        contribution
                            .ContributionCycleId))
                .GroupBy(contribution =>
                    contribution
                        .ContributionCycleId)
                .Select(group =>
                    new
                    {
                        CycleId = group.Key,

                        Amount =
                            group.Sum(
                                contribution =>
                                    contribution.Amount)
                    })
                .ToDictionaryAsync(
                    item => item.CycleId,
                    item => item.Amount);

        
            var response = stokvels
                .Select(item =>
                {
                    var hasCycle = item.CycleId.HasValue;

                    var paid =
                        hasCycle &&
                        payments.TryGetValue(
                            item.CycleId!.Value,
                            out var amount)
                            ? amount
                            : 0m;

                    var status = !hasCycle
                        ? null
                        : item.CycleClosedAtUtc.HasValue
                            ? "Closed"
                            : item.CycleStatus?.ToString();

                    var isOpen = status == "Open";

                    var stillOwed = Math.Max(
                        0m,
                        item.MonthlyContribution - paid);

                    return new MemberStokvelHomeResponse(
                        item.StokvelId,
                        item.Name,
                        item.Role.ToString(),
                        hasCycle ? item.CurrentCycle : null,
                        status,
                        item.CycleDueDate,
                        isOpen ? ToCents(paid) : null,
                        isOpen ? ToCents(stillOwed) : null,
                        isOpen
                            ? CalculateNextPayoutDate(
                                item.CycleStatus)
                            : null);
                })
                .ToList();
                return new MemberHomeResponse(response);
    }

    private static long ToCents(
        decimal amount)
    {
        return checked(
            (long)decimal.Round(
                amount * 100m,
                0,
                MidpointRounding
                    .AwayFromZero));
    }

    private DateTime CalculateNextPayoutDate(
        ContributionCycleStatus? status)
    {
        var now =
            _timeProvider
                .GetUtcNow()
                .UtcDateTime;

        var payoutMonth =
            status ==
                ContributionCycleStatus.PaidOut
                ? now.AddMonths(1)
                : now;

        var lastDay =
            DateTime.DaysInMonth(
                payoutMonth.Year,
                payoutMonth.Month);

        return new DateTime(
            payoutMonth.Year,
            payoutMonth.Month,
            lastDay,
            0,
            0,
            0,
            DateTimeKind.Utc);
    }
}