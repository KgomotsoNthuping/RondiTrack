namespace Api.DTO;

public sealed record CreateStokvelInviteResponse(
    string Code,
    DateTime ExpiresAtUtc);

public sealed record StokvelInvitePreviewResponse(
    Guid StokvelId,
    string Name,
    string? Rules,
    long ContributionAmountInCents,
    int CurrentPeriod,
    int TotalPeriods,
    DateOnly? NextContributionDueDate,
    int MemberCount,
    bool AlreadyMember);

public sealed record JoinStokvelResponse(
    Guid StokvelId,
    string Name,
    string Role);
