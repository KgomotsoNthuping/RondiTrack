namespace Api.DTO;

public sealed record MemberHomeResponse(IReadOnlyList<MemberStokvelHomeResponse> Stokvels);

public sealed record MemberStokvelHomeResponse(
    Guid StokvelId,
    string Name,
    string Role,
    int? CurrentCycle,
    string? CycleStatus,
    DateOnly? CycleDueDate,
    long? PaidThisCycleInCents,
    long? StillOwedInCents,
    DateTime? NextPayoutDateUtc);