
namespace Api.DTO;

public sealed record StokvelCycleSummaryResponse(
    Guid Id,
    int CycleNumber,
    string Status,
    DateOnly? DueDate,
    long TargetAmountInCents,
    long TotalCollectedInCents,
    long AmountOutstandingInCents);

public sealed record StokvelDetailResponse(
    Guid Id,
    string Name,
    long MonthlyContributionInCents,
    string Role,
    int MemberCount,
    int CurrentPeriod,
    int TotalPeriods,
    string? Rules,
    StokvelCycleSummaryResponse? CurrentCycle);
