namespace Api.Data;

public sealed class InMemoryIdempotency
    : IIdempotency
{
    private readonly Dictionary<string, IdempotencyRecord>
        _records = new(StringComparer.Ordinal);

    public Task<IdempotencyRecord?> GetAsync(
        string key)
    {
        _records.TryGetValue(
            key,
            out var record);

        return Task.FromResult(record);
    }

    public Task SaveAsync(
        string key,
        IdempotencyRecord record)
    {
        _records[key] = record;

        return Task.CompletedTask;
    }
}