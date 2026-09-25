namespace Api.Data;

public interface IIdempotency
{
    Task<IdempotencyRecord?> GetAsync(
        string key);

    Task SaveAsync(
        string key,
        IdempotencyRecord record);
}