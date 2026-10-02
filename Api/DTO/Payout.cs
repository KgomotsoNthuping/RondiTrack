namespace Api.DTO;

public record PayoutResponse(
    Guid Id,
    Guid StokvelId,
    Guid ContributionCycleId,
    Guid RecipientUserId,
    decimal Amount,
    DateTime PaidAtUtc);