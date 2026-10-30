namespace Api.DTO;

public sealed record ContributionHistoryPageRequest(
    int PageSize,
    Guid? Member,
    DateTime? FromUtc,
    DateTime? ToUtc,
    string? PageToken);
