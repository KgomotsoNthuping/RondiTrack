namespace Api.DTO;

public record PayoutResponse(
    Guid Id,
    Guid StokvelId,
    Guid ContributionCycleId,
    Guid RecipientMemberId,
    decimal Amount,
    DateTime PaidAtUtc);