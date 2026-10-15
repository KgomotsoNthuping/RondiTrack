namespace Domain.Repositories;

public interface ICycleCloseRepository
{
    Task<bool> TryCloseCycleAsync(
        Guid cycleId,
        DateTime closedAtUtc,
        CancellationToken cancellationToken = default);
}