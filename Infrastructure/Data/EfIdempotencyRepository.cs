using Domain.Models;
using Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Data;

public sealed class EfIdempotencyRepository
    : IIdempotencyRepository
{
    private readonly RondiTrackDbContext _dbContext;
    private readonly TimeProvider _timeProvider;

    public EfIdempotencyRepository(
        RondiTrackDbContext dbContext,
        TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _timeProvider = timeProvider;
    }

    public async Task<IdempotencyRecord?> GetAsync(
        string key,
        CancellationToken cancellationToken = default)
    {
        var entry =
            await _dbContext.IdempotencyEntries
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    value => value.Key == key,
                    cancellationToken);

        if (entry is null)
        {
            return null;
        }

        var contribution =
            await _dbContext.Contributions
                .AsNoTracking()
                .SingleAsync(
                    value =>
                        value.Id ==
                        entry.ContributionId,
                    cancellationToken);

        var signature =
            new ContributionRequestSignature(
                entry.StokvelId,
                entry.UserId,
                entry.ContributionCycleId,
                entry.Amount);

        return new IdempotencyRecord(
            signature,
            contribution);
    }

    public async Task SaveAsync(
        string key,
        IdempotencyRecord record,
        CancellationToken cancellationToken = default)
    {
        var now =
            _timeProvider
                .GetUtcNow()
                .UtcDateTime;

        var entry =
            new IdempotencyEntry(
                key,
                record.Signature.StokvelId,
                record.Signature.UserId,
                record.Signature
                    .ContributionCycleId,
                record.Signature.Amount,
                record.Contribution.Id,
                now);

        _dbContext.IdempotencyEntries
            .Add(entry);

        await _dbContext.SaveChangesAsync(
            cancellationToken);
    }

    public async Task<int>
        DeleteCreatedBeforeAsync(
            DateTime cutoff,
            CancellationToken cancellationToken = default)
    {
        return await _dbContext
            .IdempotencyEntries
            .Where(entry =>
                entry.CreatedAtUtc < cutoff)
            .ExecuteDeleteAsync(
                cancellationToken);
    }
}