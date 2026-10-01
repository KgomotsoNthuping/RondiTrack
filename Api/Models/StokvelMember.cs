namespace Api.Models;

public class StokvelMember
{
    public Guid Id { get; private set; }

    public Guid StokvelId { get; private set; }

    public Guid UserId { get; private set; }

    public DateTime JoinedAtUtc { get; private set; }

    // Used by EF Core when materializing data from PostgreSQL.
    private StokvelMember() { }

    public StokvelMember(
        Guid stokvelId,
        Guid userId,
        DateTime? joinedAtUtc = null)
    {
        if (stokvelId == Guid.Empty)
        {
            throw new ArgumentException("A stokvel ID is required.");
        }

        if (userId == Guid.Empty)
        {
            throw new ArgumentException("A user ID is required.");
        }

        Id = Guid.NewGuid();
        StokvelId = stokvelId;
        UserId = userId;
        JoinedAtUtc = joinedAtUtc ?? DateTime.UtcNow;
    }
}