namespace Domain.Models;

public sealed class RefreshToken
{
    public Guid Id { get; private set; }

    public Guid UserId { get; private set; }

    public string TokenHash { get; private set; } =
        string.Empty;

    public Guid FamilyId { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public DateTime ExpiresAtUtc { get; private set; }

    public DateTime? RevokedAtUtc { get; private set; }

    public Guid? ReplacedByTokenId { get; private set; }

    public uint Version { get; private set; }

    private RefreshToken()
    {
    }

    public RefreshToken(
        Guid userId,
        string tokenHash,
        Guid familyId,
        DateTime createdAtUtc,
        DateTime expiresAtUtc)
    {
        if (string.IsNullOrWhiteSpace(tokenHash))
        {
            throw new ArgumentException(
                "A refresh token hash is required.");
        }

        Id = Guid.NewGuid();
        UserId = userId;
        TokenHash = tokenHash;
        FamilyId = familyId;
        CreatedAtUtc = createdAtUtc;
        ExpiresAtUtc = expiresAtUtc;
    }

    public bool IsExpired(DateTime now)
    {
        return now >= ExpiresAtUtc;
    }

    public void Rotate(
        Guid replacementTokenId,
        DateTime now)
    {
        RevokedAtUtc = now;
        ReplacedByTokenId =
            replacementTokenId;
    }
}