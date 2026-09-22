namespace RondiTrack.Api.Contracts;

public sealed class RecordContributionRequest
{
    public Guid UserId { get; init; }

    public int CycleNumber { get; init; }

    public decimal Amount { get; init; }
}

public sealed record ContributionResponse(
    Guid Id,
    Guid StokvelId,
    Guid UserId,
    int CycleNumber,
    decimal Amount,
    DateTime RecordedAtUtc);