using Api.Background;
using Api.DTO;
using Domain.Exceptions;
using Domain.Models;
using Domain.Repositories;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Api.Services;

public sealed class ManualCycleCloseService
{
    private readonly RondiTrackDbContext _db;
    private readonly CycleClosePlanner _planner;
    private readonly ICycleCloseRepository _closer;
    private readonly ICycleSchedule _schedule;
    private readonly TimeProvider _clock;

    public ManualCycleCloseService(
        RondiTrackDbContext db,
        CycleClosePlanner planner,
        ICycleCloseRepository closer,
        ICycleSchedule schedule,
        TimeProvider clock)
    {
        _db = db;
        _planner = planner;
        _closer = closer;
        _schedule = schedule;
        _clock = clock;
    }

    public async Task<CycleClosePreviewResponse> PreviewAsync(
        Guid stokvelId,
        Guid cycleId,
        Guid callerUserId,
        CancellationToken cancellationToken = default)
    {
        await EnsureTreasurerAsync(
            stokvelId,
            callerUserId,
            cancellationToken);

        var stokvel = await _db.Stokvels
            .AsNoTracking()
            .SingleOrDefaultAsync(
                item => item.Id == stokvelId,
                cancellationToken);

        var cycle = await _db.ContributionCycles
            .AsNoTracking()
            .SingleOrDefaultAsync(
                item =>
                    item.Id == cycleId &&
                    item.StokvelId == stokvelId,
                cancellationToken);

        if (stokvel is null || cycle is null)
        {
            throw new ResourceNotFoundException(
                "The contribution cycle was not found.");
        }

        await EnsureClosableAsync(
            stokvelId,
            cycle,
            cancellationToken);

        var localDate = _schedule.GetLocalDate(
            _clock.GetUtcNow());

        return await _planner.CalculateAsync(
            stokvel,
            cycle,
            localDate,
            cancellationToken);
    }

    public async Task<CloseCycleResponse> CloseAsync(
        Guid stokvelId,
        Guid cycleId,
        Guid callerUserId,
        string idempotencyKey,
        string previewVersion,
        CancellationToken cancellationToken = default)
    {
        if (
            string.IsNullOrWhiteSpace(idempotencyKey) ||
            idempotencyKey.Length > 200 ||
            idempotencyKey.Any(char.IsWhiteSpace)
        )
        {
            throw Invalid(
                "idempotencyKey",
                "Provide a valid Idempotency-Key.");
        }

        if (
            string.IsNullOrWhiteSpace(previewVersion) ||
            previewVersion.Length != 64 ||
            previewVersion.Any(character =>
                !Uri.IsHexDigit(character))
        )
        {
            throw Invalid(
                "previewVersion",
                "Provide the version from the close preview.");
        }

        var expectedVersion =
            previewVersion.ToUpperInvariant();

        var strategy = _db.Database
            .CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            await using var transaction =
                await _db.Database.BeginTransactionAsync(
                    cancellationToken);

            await EnsureTreasurerAsync(
                stokvelId,
                callerUserId,
                cancellationToken);

            var locked = await CycleRowLock.LockAsync(
                _db,
                stokvelId,
                cycleId,
                cancellationToken);

            if (!locked)
            {
                throw new ResourceNotFoundException(
                    "The contribution cycle was not found.");
            }

            var previous = await _db.CycleCloseReceipts
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    item =>
                        item.CallerUserId == callerUserId &&
                        item.Key == idempotencyKey,
                    cancellationToken);

            if (previous is not null)
            {
                if (
                    previous.StokvelId != stokvelId ||
                    previous.CycleId != cycleId ||
                    previous.PreviewVersion != expectedVersion
                )
                {
                    throw new ConflictException(
                        "This Idempotency-Key was used for a different close request.");
                }

                return ToResponse(previous);
            }

            var stokvel = await _db.Stokvels
                .SingleOrDefaultAsync(
                    item => item.Id == stokvelId,
                    cancellationToken);

            var cycle = await _db.ContributionCycles
                .AsNoTracking()
                .SingleAsync(
                    item =>
                        item.Id == cycleId &&
                        item.StokvelId == stokvelId,
                    cancellationToken);

            if (stokvel is null)
            {
                throw new ResourceNotFoundException(
                    "The stokvel was not found.");
            }

            await EnsureClosableAsync(
                stokvelId,
                cycle,
                cancellationToken);

            var now = _clock.GetUtcNow();
            var localDate = _schedule.GetLocalDate(now);

            var current = await _planner.CalculateAsync(
                stokvel,
                cycle,
                localDate,
                cancellationToken);

            if (
                !string.Equals(
                    current.Version,
                    expectedVersion,
                    StringComparison.Ordinal)
            )
            {
                throw new ConflictException(
                    "The cycle has changed since the preview. Load a new preview and confirm again.");
            }

            var nextCycleNumber = checked(
                cycle.CycleNumber + 1);

            var nextAlreadyExists =
                await _db.ContributionCycles
                    .AsNoTracking()
                    .AnyAsync(
                        item =>
                            item.StokvelId == stokvelId &&
                            item.CycleNumber == nextCycleNumber,
                        cancellationToken);

            if (nextAlreadyExists)
            {
                throw new ConflictException(
                    "The next contribution cycle already exists.");
            }

            var closedAtUtc = now.UtcDateTime;

            var claimed = await _closer.TryCloseCycleAsync(
                cycleId,
                closedAtUtc,
                cancellationToken);

            if (!claimed)
            {
                throw new ConflictException(
                    "This cycle has already been closed.");
            }

            foreach (var item in current.Arrears)
            {
                _db.Arrears.Add(
                    new Arrear(
                        stokvelId,
                        item.UserId,
                        cycleId,
                        item.ShortfallInCents,
                        closedAtUtc));
            }

            var nextEndDate = _schedule.GetNextEndDate(
                cycle.EndDate!.Value);

            _db.ContributionCycles.Add(
                new ContributionCycle(
                    stokvelId,
                    nextCycleNumber,
                    cycle.TargetAmount,
                    nextEndDate));

            var nextPeriod =
                stokvel.CurrentPeriod >= stokvel.TotalPeriods
                    ? 1
                    : stokvel.CurrentPeriod + 1;

            stokvel.UpdateDetails(
                stokvel.Name,
                stokvel.MonthlyContribution,
                nextPeriod,
                stokvel.TotalPeriods,
                stokvel.Rules);

            var receipt = new CycleCloseReceipt(
                stokvelId,
                cycleId,
                callerUserId,
                idempotencyKey,
                expectedVersion,
                localDate,
                closedAtUtc,
                current.Arrears.Count,
                current.TotalArrearsInCents);

            _db.CycleCloseReceipts.Add(receipt);

            await _db.SaveChangesAsync(
                cancellationToken);

            await transaction.CommitAsync(
                cancellationToken);

            return ToResponse(receipt);
        });
    }

    private async Task EnsureTreasurerAsync(
        Guid stokvelId,
        Guid callerUserId,
        CancellationToken cancellationToken)
    {
        var authorised = await _db.StokvelMembers
            .AsNoTracking()
            .AnyAsync(
                member =>
                    member.StokvelId == stokvelId &&
                    member.UserId == callerUserId &&
                    member.Role == StokvelMemberRole.Treasurer,
                cancellationToken);

        if (!authorised)
        {
            throw new CycleClosePermissionException();
        }
    }

    private async Task EnsureClosableAsync(
        Guid stokvelId,
        ContributionCycle cycle,
        CancellationToken cancellationToken)
    {
        if (
            cycle.ClosedAtUtc.HasValue ||
            cycle.Status != ContributionCycleStatus.Open
        )
        {
            throw new ConflictException(
                "This contribution cycle is already closed.");
        }

        if (cycle.EndDate is null)
        {
            throw new ConflictException(
                "This cycle does not have a scheduled end date.");
        }

        var earlierOpenCycle =
            await _db.ContributionCycles
                .AsNoTracking()
                .AnyAsync(
                    item =>
                        item.StokvelId == stokvelId &&
                        item.CycleNumber < cycle.CycleNumber &&
                        item.ClosedAtUtc == null,
                    cancellationToken);

        if (earlierOpenCycle)
        {
            throw new ConflictException(
                "An earlier contribution cycle must be closed first.");
        }
    }

    private static CloseCycleResponse ToResponse(
        CycleCloseReceipt receipt)
    {
        return new CloseCycleResponse(
            receipt.StokvelId,
            receipt.CycleId,
            receipt.AsOfDate,
            receipt.PreviewVersion,
            receipt.ClosedAtUtc,
            receipt.ArrearsCount,
            receipt.TotalArrearsInCents);
    }

    private static ValidationException Invalid(
        string field,
        string message)
    {
        return new ValidationException(
            new Dictionary<string, string[]>
            {
                [field] = [message]
            });
    }
}
