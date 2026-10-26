namespace Api.DTO;

public sealed class RecordCycleContributionRequest
{
    public Guid UserId { get; init; }

    public long AmountInCents { get; init; }
}

public sealed record RecordedCycleContributionResponse(
    Guid Id,
    Guid StokvelId,
    Guid UserId,
    Guid ContributionCycleId,
    long AmountInCents,
    string Status,
    DateTime? PaidAtUtc);
