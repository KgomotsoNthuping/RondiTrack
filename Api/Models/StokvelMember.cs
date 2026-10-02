using System.Text.Json.Serialization;

namespace Api.Models;

public class StokvelMember
{
    public Guid StokvelId { get; private set; }

    public Guid UserId { get; private set; }

    public DateTime JoinedAtUtc { get; private set; } 

    public StokvelMemberRole Role { get; private set; }

    [JsonIgnore]
    public User User { get; private set; } = null!;

    [JsonIgnore]
    public Stokvel Stokvel { get; private set; } = null!;

    [JsonIgnore]
    public ICollection<Contribution> Contributions { get; private set; } = new List<Contribution>();

    [JsonIgnore]
    public ICollection<Payout> Payouts { get; private set; } = new List<Payout>();

    // Used by EF Core when materializing data from PostgreSQL.
    private StokvelMember() { }

    public StokvelMember(
        Guid stokvelId,
        Guid userId,
        StokvelMemberRole role = StokvelMemberRole.Member,
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
        StokvelId = stokvelId;
        UserId = userId;
        Role = role;
        JoinedAtUtc = joinedAtUtc ?? DateTime.UtcNow;
    }
}