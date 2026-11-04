
namespace Domain.Models;

public sealed class ContributionProof
{
    public Guid Id { get; private set; }

    public Guid StokvelId { get; private set; }

    public Guid ContributionId { get; private set; }

    public Guid UploadedByUserId { get; private set; }

    public string ContentType { get; private set; } =
        string.Empty;

    public long SizeBytes { get; private set; }

    public byte[] Content { get; private set; } = [];

    public DateTime UploadedAtUtc { get; private set; }

    private ContributionProof()
    {
    }

    public ContributionProof(
        Guid stokvelId,
        Guid contributionId,
        Guid uploadedByUserId,
        string contentType,
        byte[] content,
        DateTime uploadedAtUtc)
    {
        if (
            stokvelId == Guid.Empty ||
            contributionId == Guid.Empty ||
            uploadedByUserId == Guid.Empty
        )
        {
            throw new ArgumentException(
                "Valid contribution and uploader IDs are required.");
        }

        Id = Guid.NewGuid();
        StokvelId = stokvelId;
        ContributionId = contributionId;

        Replace(
            uploadedByUserId,
            contentType,
            content,
            uploadedAtUtc);
    }

    public void Replace(
        Guid uploadedByUserId,
        string contentType,
        byte[] content,
        DateTime uploadedAtUtc)
    {
        if (uploadedByUserId == Guid.Empty)
        {
            throw new ArgumentException(
                "A valid uploader ID is required.");
        }

        if (uploadedAtUtc.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException(
                "The upload timestamp must be UTC.");
        }

        if (
            contentType is not
                ("application/pdf" or
                 "image/png" or
                 "image/jpeg")
        )
        {
            throw new ArgumentException(
                "Unsupported proof content type.");
        }

        if (
            content.Length == 0 ||
            content.Length > 5 * 1024 * 1024
        )
        {
            throw new ArgumentException(
                "Invalid proof file size.");
        }

        UploadedByUserId = uploadedByUserId;
        ContentType = contentType;
        Content = content.ToArray();
        SizeBytes = content.LongLength;
        UploadedAtUtc = uploadedAtUtc;
    }
}
