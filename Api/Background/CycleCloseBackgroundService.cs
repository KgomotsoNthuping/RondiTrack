using System.Diagnostics;
using Api.Observability;
using Domain.Models;
using Domain.Repositories;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Api.Background;

public sealed class CycleCloseBackgroundService
    : BackgroundService
{
    public const string ActivitySourceName =
        "RondiTrack.Jobs";

    private static readonly ActivitySource
        ActivitySource =
            new(ActivitySourceName);

    private readonly ICycleSchedule _schedule;

    private readonly IServiceScopeFactory
        _scopeFactory;

    private readonly TimeProvider
        _timeProvider;

    private readonly CycleCloseOptions
        _options;

    private readonly CycleCloseMetrics
        _metrics;

    private readonly ILogger<
        CycleCloseBackgroundService> _logger;

    public CycleCloseBackgroundService(
        IServiceScopeFactory scopeFactory,
        TimeProvider timeProvider,
        ICycleSchedule schedule,
        CycleCloseOptions options,
        CycleCloseMetrics metrics,
        ILogger<
            CycleCloseBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _schedule = schedule;
        _timeProvider = timeProvider;
        _options = options;
        _metrics = metrics;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        using var timer =
            new PeriodicTimer(
                _options.Interval,
                _timeProvider);

        try
        {
            do
            {
                await RunOnceAsync(
                    stoppingToken);
            }
            while (
                await timer.WaitForNextTickAsync(
                    stoppingToken));
        }
        catch (OperationCanceledException)
            when (stoppingToken
                .IsCancellationRequested)
        {
        }
    }

    public Task<CycleCloseRunResult> RunOnceAsync(
        CancellationToken cancellationToken = default)
    {
        return RunOnceCoreAsync(null, cancellationToken);
    }

    public Task<CycleCloseRunResult> RunForStokvelsAsync(
        IReadOnlyCollection<Guid> stokvelIds,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stokvelIds);
        return RunOnceCoreAsync(stokvelIds.ToArray(), cancellationToken);
    }

    private async Task<CycleCloseRunResult> RunOnceCoreAsync(
        Guid[]? allowedStokvelIds,
        CancellationToken cancellationToken)
    {
        using var activity =
            ActivitySource.StartActivity(
                "cycle-close");

        try
        {
            var now =
                _timeProvider.GetUtcNow();

            var localDate =
                _schedule.GetLocalDate(
                    now);

            List<Guid> stokvelIds;

            await using (
                var scope =
                    _scopeFactory
                        .CreateAsyncScope())
            {
                var db =
                    scope.ServiceProvider
                        .GetRequiredService<
                            RondiTrackDbContext>();

                IQueryable<ContributionCycle> eligibleCycles =
                    db.ContributionCycles
                        .AsNoTracking()
                        .Where(cycle =>
                            cycle.ClosedAtUtc == null &&
                            cycle.EndDate != null &&
                            cycle.EndDate < localDate);

                if (allowedStokvelIds is not null)
                {
                    eligibleCycles = eligibleCycles.Where(cycle =>
                        allowedStokvelIds.Contains(cycle.StokvelId));
                }

                stokvelIds = await eligibleCycles
                    .Select(cycle => cycle.StokvelId)
                    .Distinct()
                    .ToListAsync(cancellationToken);
            }

            var cyclesClosed =
                0;

            var arrearsRecorded =
                0;

            foreach (
                var stokvelId in stokvelIds)
            {
                try
                {
                    var result =
                        await ProcessStokvelAsync(
                            stokvelId,
                            now,
                            cancellationToken);

                    cyclesClosed +=
                        result.CyclesClosed;

                    arrearsRecorded +=
                        result.ArrearsRecorded;
                }
                catch (Exception exception)
                    when (
                        exception
                            is not
                            OperationCanceledException)
                {
                    _logger.LogError(
                        exception,
                        "Cycle close failed for stokvel {StokvelId}. It will be retried.",
                        stokvelId);
                }
            }

            _metrics.RecordCyclesClosed(
                cyclesClosed);

            _metrics.RecordArrears(
                arrearsRecorded);

            activity?.SetTag(
                "ronditrack.cycles_closed",
                cyclesClosed);

            activity?.SetTag(
                "ronditrack.arrears_recorded",
                arrearsRecorded);

            return new CycleCloseRunResult(
                cyclesClosed,
                arrearsRecorded);
        }
        catch (Exception exception)
            when (
                exception
                    is not
                    OperationCanceledException)
        {
            activity?.SetStatus(
                ActivityStatusCode.Error);

            _logger.LogError(
                exception,
                "Cycle close run failed. It will retry on the next interval.");

            return new CycleCloseRunResult(
                0,
                0);
        }
    }

    private async Task<CycleCloseRunResult>
        ProcessStokvelAsync(
            Guid stokvelId,
            DateTimeOffset now,
            CancellationToken cancellationToken)
    {
        await using var scope =
            _scopeFactory.CreateAsyncScope();

        var db =
            scope.ServiceProvider
                .GetRequiredService<
                    RondiTrackDbContext>();

        var cycleCloser =
            scope.ServiceProvider
                .GetRequiredService<
                    ICycleCloseRepository>();

        var executionStrategy =
            db.Database
                .CreateExecutionStrategy();

        return await executionStrategy
            .ExecuteAsync(
                async () =>
                {
                    await using var transaction =
                        await db.Database
                            .BeginTransactionAsync(
                                cancellationToken);

                    var stokvel =
                        await db.Stokvels
                            .SingleAsync(
                                value =>
                                    value.Id ==
                                    stokvelId,
                                cancellationToken);

                    var cyclesClosed =
                        0;

                    var arrearsRecorded =
                        0;

                    while (true)
                    {
                        var cycle =
                            await db
                                .ContributionCycles
                                .AsNoTracking()
                                .Where(value =>
                                    value.StokvelId ==
                                        stokvelId &&
                                    value.ClosedAtUtc ==
                                        null &&
                                    value.EndDate !=
                                        null)
                                .OrderBy(value =>
                                    value.CycleNumber)
                                .FirstOrDefaultAsync(
                                    cancellationToken);

                        if (cycle is null)
                        {
                            break;
                        }

                        var closeInstant =
                            _schedule.GetCloseInstantUtc(
                                cycle.EndDate.Value);

                        if (now <
                            closeInstant)
                        {
                            break;
                        }

                        var claimed =
                            await cycleCloser
                                .TryCloseCycleAsync(
                                    cycle.Id,
                                    closeInstant
                                        .UtcDateTime,
                                    cancellationToken);

                        if (!claimed)
                        {
                            await transaction
                                .RollbackAsync(
                                    cancellationToken);

                            return new
                                CycleCloseRunResult(
                                    0,
                                    0);
                        }

                        var memberIds =
                            await db
                                .StokvelMembers
                                .Where(member =>
                                    member.StokvelId ==
                                        stokvelId)
                                .Select(member =>
                                    member.UserId)
                                .ToListAsync(
                                    cancellationToken);

                        var contributions =
                            await db
                                .Contributions
                                .Where(contribution =>
                                    contribution
                                        .StokvelId ==
                                            stokvelId &&
                                    contribution
                                        .ContributionCycleId ==
                                            cycle.Id)
                                .GroupBy(
                                    contribution =>
                                        contribution
                                            .UserId)
                                .Select(group =>
                                    new
                                    {
                                        UserId =
                                            group.Key,

                                        Paid =
                                            group.Sum(
                                                value =>
                                                    value.Amount)
                                    })
                                .ToDictionaryAsync(
                                    value =>
                                        value.UserId,
                                    value =>
                                        value.Paid,
                                    cancellationToken);

                        var requiredCents =
                            ToCents(
                                stokvel
                                    .MonthlyContribution);

                        foreach (
                            var userId in memberIds)
                        {
                            contributions
                                .TryGetValue(
                                    userId,
                                    out var paid);

                            var paidCents =
                                ToCents(paid);

                            var shortfall =
                                requiredCents -
                                paidCents;

                            if (shortfall <= 0)
                            {
                                continue;
                            }

                            db.Arrears.Add(
                                new Arrear(
                                    stokvelId,
                                    userId,
                                    cycle.Id,
                                    shortfall,
                                    closeInstant
                                        .UtcDateTime));

                            arrearsRecorded++;
                        }

                        var nextCycleNumber =
                            cycle.CycleNumber + 1;

                        var nextEndDate =
                            _schedule.GetNextEndDate(
                                cycle.EndDate.Value);

                        db.ContributionCycles.Add(
                            new ContributionCycle(
                                stokvelId,
                                nextCycleNumber,
                                cycle.TargetAmount,
                                nextEndDate));

                        var nextPeriod =
                            stokvel.CurrentPeriod >=
                            stokvel.TotalPeriods
                                ? 1
                                : stokvel.CurrentPeriod +
                                    1;

                        stokvel.UpdateDetails(
                            stokvel.Name,
                            stokvel.MonthlyContribution,
                            nextPeriod,
                            stokvel.TotalPeriods,
                            stokvel.Rules);

                        await db.SaveChangesAsync(
                            cancellationToken);

                        cyclesClosed++;
                    }

                    await transaction.CommitAsync(
                        cancellationToken);

                    return new
                        CycleCloseRunResult(
                            cyclesClosed,
                            arrearsRecorded);
                });
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
}

public sealed record CycleCloseRunResult(
    int CyclesClosed,
    int ArrearsRecorded);
