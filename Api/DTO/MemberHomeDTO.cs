namespace Api.DTO;

public sealed record MemberHomeResponse(IReadOnlyList<MemberStokvelHomeResponse> Stokvels);

public sealed record MemberStokvelHomeResponse(
    string Name,
    string Role,
    int CurrentCycle,
    long PaidThisCycleInCents,
    long StillOwedInCents,
    DateTime NextPayoutDateUtc);