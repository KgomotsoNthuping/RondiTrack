using Api.Models;

namespace Api.Data;

// Represents the important parts of a contribution request.
public sealed record ContributionRequestSignature(
    Guid StokvelId,
    Guid UserId,
    Guid ContributionCycleId,
    decimal Amount);

// Stores what request used an Idempotency-Key
// and the contribution created by that request.
public sealed record IdempotencyRecord(
    ContributionRequestSignature Signature,
    Contribution Contribution);