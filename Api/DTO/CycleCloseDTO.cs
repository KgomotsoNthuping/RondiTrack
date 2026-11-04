
namespace Api.DTO;

public sealed record CycleCloseArrearResponse(
    Guid UserId,
    string MemberName,
    long ShortfallInCents);

public sealed record CycleClosePreviewResponse(
    Guid StokvelId,
    Guid CycleId,
    int CycleNumber,
    DateOnly AsOfDate,
    string Version,
    IReadOnlyList<CycleCloseArrearResponse> Arrears,
    long TotalArrearsInCents);

public sealed record CloseCycleRequest(
    string PreviewVersion);

public sealed record CloseCycleResponse(
    Guid StokvelId,
    Guid CycleId,
    DateOnly AsOfDate,
    string PreviewVersion,
    DateTime ClosedAtUtc,
    int ArrearsCount,
    long TotalArrearsInCents);
