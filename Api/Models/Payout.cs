using System.Text.Json.Serialization;

namespace Api.Models;

public class Payout
{
    public Guid Id { get; private set; }

    public Guid StokvelId { get; private set; }

    public Guid ContributionCycleId { get; private set; }

    public decimal Amount { get; private set; }

    public DateTime PaidAtUtc { get; private set; }

    public Guid RecipientUserId { get; private set; }

    [JsonIgnore]
    public StokvelMember RecipientMember { get; private set; } = null!;

    private Payout()
    {
    }

    public Payout(
        Guid stokvelId,
        Guid contributionCycleId,
        Guid recipientUserId,
        decimal amount)
    {
        if (stokvelId == Guid.Empty)
        {
            throw new ArgumentException("A stokvel ID is required.");
        }

        if (contributionCycleId == Guid.Empty)
        {
            throw new ArgumentException("A contribution cycle ID is required.");
        }

        if (amount <= 0)
        {
            throw new ArgumentException("The payout amount must be greater than zero.");
        }

        if (recipientUserId == Guid.Empty)
        {
            throw new ArgumentException("A recipient user ID is required.");
        }

        Id = Guid.NewGuid();
        StokvelId = stokvelId;
        ContributionCycleId = contributionCycleId;
        Amount = amount;
        PaidAtUtc = DateTime.UtcNow;
        RecipientUserId = recipientUserId;
    }
}