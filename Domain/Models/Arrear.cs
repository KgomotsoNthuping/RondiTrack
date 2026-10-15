namespace Domain.Models;

public sealed class Arrear
{
    public Guid Id { get; private set; }

    public Guid StokvelId { get; private set; }

    public Guid UserId { get; private set; }

    public Guid ContributionCycleId { get; private set; }

    public long ShortfallInCents { get; private set; }

    public DateTime RecordedAtUtc { get; private set; }

    private Arrear()
    {
    }

    public Arrear(
        Guid stokvelId,
        Guid userId,
        Guid contributionCycleId,
        long shortfallInCents,
        DateTime recordedAtUtc)
    {
        if (stokvelId == Guid.Empty)
        {
            throw new ArgumentException(
                "A stokvel ID is required.");
        }

        if (userId == Guid.Empty)
        {
            throw new ArgumentException(
                "A user ID is required.");
        }

        if (contributionCycleId == Guid.Empty)
        {
            throw new ArgumentException(
                "A contribution cycle ID is required.");
        }

        if (shortfallInCents <= 0)
        {
            throw new ArgumentException(
                "The arrear shortfall must be greater than zero.");
        }

        if (recordedAtUtc.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException(
                "RecordedAtUtc must be UTC.");
        }

        Id = Guid.NewGuid();
        StokvelId = stokvelId;
        UserId = userId;
        ContributionCycleId = contributionCycleId;
        ShortfallInCents = shortfallInCents;
        RecordedAtUtc = recordedAtUtc;
    }
}