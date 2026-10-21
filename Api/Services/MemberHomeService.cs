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
                    contribution.UserId == userId &&
                    contribution.Status != ContributionStatus.Pending &&
                    cycleIds.Contains(contribution.ContributionCycleId))
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

        var stokvelIds = stokvels
            .Select(item => item.StokvelId)
            .Distinct()
            .ToList();

        // Only the signed-in member's recorded shortfalls are returned.
        var arrearsByStokvel = await _dbContext
            .Arrears
            .AsNoTracking()
            .Where(arrear =>
                arrear.UserId == userId &&
                stokvelIds.Contains(arrear.StokvelId))
            .GroupBy(arrear => arrear.StokvelId)
            .Select(group => new
            {
                StokvelId = group.Key,
                TotalInCents = group.Sum(arrear => arrear.ShortfallInCents)
            })
            .ToDictionaryAsync(
                item => item.StokvelId,
                item => item.TotalInCents);

        var response = stokvels
            .Select(item =>
            {
                var hasCycle = item.CycleId.HasValue;

                var paid = hasCycle &&
                    payments.TryGetValue(item.CycleId!.Value, out var amount)
                        ? amount
                        : 0m;

                var status = !hasCycle
                    ? null
                    : item.CycleStatus == ContributionCycleStatus.PaidOut
                        ? "PaidOut"
                        : item.CycleClosedAtUtc.HasValue
                            ? "Closed"
                            : "Open";

                var isOpen = status == "Open";
                var stillOwed = Math.Max(0m, item.MonthlyContribution - paid);
                var arrearsInCents = arrearsByStokvel.TryGetValue(
                    item.StokvelId,
                    out var total)
                        ? total
                        : 0L;

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
                        ? CalculateNextPayoutDate(item.CycleStatus)
                        : null,
                    arrearsInCents);
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