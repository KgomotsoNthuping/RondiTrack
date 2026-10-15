namespace Domain.Models;

public sealed record ContributionRequestSignature(
    Guid StokvelId,
    Guid UserId,
    Guid ContributionCycleId,
    decimal Amount);

public sealed record IdempotencyRecord(
    ContributionRequestSignature Signature,
    Contribution Contribution);