namespace Domain.Models;

public sealed class StokvelInvite
{
    public Guid Id { get; private set; }

    public Guid StokvelId { get; private set; }

    public Guid CreatedByUserId { get; private set; }

    public string CodeHash { get; private set; } = string.Empty;

    public DateTime CreatedAtUtc { get; private set; }

    public DateTime ExpiresAtUtc { get; private set; }

    public DateTime? RevokedAtUtc { get; private set; }

    private StokvelInvite()
    {
    }

    public StokvelInvite(
        Guid stokvelId,
        Guid createdByUserId,
        string codeHash,
        DateTime createdAtUtc,
        DateTime expiresAtUtc)
    {
        if (stokvelId == Guid.Empty ||
            createdByUserId == Guid.Empty)
        {
            throw new ArgumentException(
                "A stokvel and creator are required.");
        }

        if (codeHash.Length != 64)
        {
            throw new ArgumentException(
                "An invitation hash is required.");
        }

        if (createdAtUtc.Kind != DateTimeKind.Utc ||
            expiresAtUtc.Kind != DateTimeKind.Utc ||
            expiresAtUtc <= createdAtUtc)
        {
            throw new ArgumentException(
                "A valid UTC expiry is required.");
        }

        Id = Guid.NewGuid();
        StokvelId = stokvelId;
        CreatedByUserId = createdByUserId;
        CodeHash = codeHash;
        CreatedAtUtc = createdAtUtc;
        ExpiresAtUtc = expiresAtUtc;
    }

    public bool IsActive(DateTime nowUtc)
    {
        return RevokedAtUtc is null &&
               ExpiresAtUtc > nowUtc;
    }

    public void Revoke(DateTime nowUtc)
    {
        if (nowUtc.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException(
                "Revocation time must be UTC.");
        }

        RevokedAtUtc ??= nowUtc;
    }
}
