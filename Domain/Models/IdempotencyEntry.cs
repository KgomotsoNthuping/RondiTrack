namespace Domain.Models;

public sealed class IdempotencyEntry
{
    public Guid Id { get; private set; }

    public string Key { get; private set; }

    public Guid StokvelId { get; private set; }

    public Guid UserId { get; private set; }

    public Guid ContributionCycleId { get; private set; }

    public decimal Amount { get; private set; }

    public Guid ContributionId { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    private IdempotencyEntry()
    {
        Key = string.Empty;
    }

    public IdempotencyEntry(
        string key,
        Guid stokvelId,
        Guid userId,
        Guid contributionCycleId,
        decimal amount,
        Guid contributionId,
        DateTime createdAtUtc)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException(
                "An idempotency key is required.");
        }

        if (createdAtUtc.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException(
                "CreatedAtUtc must be UTC.");
        }

        Id = Guid.NewGuid();
        Key = key;
        StokvelId = stokvelId;
        UserId = userId;
        ContributionCycleId = contributionCycleId;
        Amount = amount;
        ContributionId = contributionId;
        CreatedAtUtc = createdAtUtc;
    }
}