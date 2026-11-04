namespace Domain.Models;

public sealed class CycleCloseReceipt
{
    public Guid Id { get; private set; }

    public Guid StokvelId { get; private set; }

    public Guid CycleId { get; private set; }

    public Guid CallerUserId { get; private set; }

    public string Key { get; private set; } = string.Empty;

    public string PreviewVersion { get; private set; } =
        string.Empty;

    public DateOnly AsOfDate { get; private set; }

    public DateTime ClosedAtUtc { get; private set; }

    public int ArrearsCount { get; private set; }

    public long TotalArrearsInCents { get; private set; }

    private CycleCloseReceipt()
    {
    }

    public CycleCloseReceipt(
        Guid stokvelId,
        Guid cycleId,
        Guid callerUserId,
        string key,
        string previewVersion,
        DateOnly asOfDate,
        DateTime closedAtUtc,
        int arrearsCount,
        long totalArrearsInCents)
    {
        if (
            stokvelId == Guid.Empty ||
            cycleId == Guid.Empty ||
            callerUserId == Guid.Empty
        )
        {
            throw new ArgumentException(
                "Valid cycle and user IDs are required.");
        }

        if (
            string.IsNullOrWhiteSpace(key) ||
            key.Length > 200 ||
            previewVersion.Length != 64 ||
            closedAtUtc.Kind != DateTimeKind.Utc
        )
        {
            throw new ArgumentException(
                "Invalid cycle-close receipt.");
        }

        Id = Guid.NewGuid();
        StokvelId = stokvelId;
        CycleId = cycleId;
        CallerUserId = callerUserId;
        Key = key;
        PreviewVersion = previewVersion;
        AsOfDate = asOfDate;
        ClosedAtUtc = closedAtUtc;
        ArrearsCount = arrearsCount;
        TotalArrearsInCents = totalArrearsInCents;
    }
}
