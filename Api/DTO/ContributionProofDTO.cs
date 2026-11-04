namespace Api.DTO;

public sealed record ContributionProofResponse(
    Guid Id,
    Guid StokvelId,
    Guid ContributionId,
    Guid UploadedByUserId,
    string ContentType,
    long SizeBytes,
    DateTime UploadedAtUtc);
