namespace Api.DTO;

public sealed record MemberActivityItemResponse(
    Guid Id,
    string Type,
    Guid StokvelId,
    string StokvelName,
    Guid MemberId,
    string MemberName,
    DateTime OccurredAtUtc,
    long? AmountInCents);
