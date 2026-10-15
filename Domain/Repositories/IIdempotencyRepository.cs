using Domain.Models;

namespace Domain.Repositories;

public interface IIdempotencyRepository
{
    Task<IdempotencyRecord?> GetAsync(
        string key,
        CancellationToken cancellationToken = default);

    Task SaveAsync(
        string key,
        IdempotencyRecord record,
        CancellationToken cancellationToken = default);

    Task<int> DeleteCreatedBeforeAsync(
        DateTime cutoff,
        CancellationToken cancellationToken = default);
}