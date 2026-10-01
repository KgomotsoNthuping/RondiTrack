namespace Api.Models;

public class Payout
{
    public Guid Id { get; private set; }

    public Guid StokvelId { get; private set; }

    public Guid ContributionCycleId { get; private set; }

    public Guid RecipientMemberId { get; private set; }

    public decimal Amount { get; private set; }

    public DateTime PaidAtUtc { get; private set; }

    private Payout() { }

    public Payout(
        Guid stokvelId,
        Guid contributionCycleId,
        Guid recipientMemberId,
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

        if (recipientMemberId == Guid.Empty)
        {
            throw new ArgumentException("A recipient member ID is required.");
        }

        if (amount <= 0)
        {
            throw new ArgumentException("The payout amount must be greater than zero.");
        }

        Id = Guid.NewGuid();
        StokvelId = stokvelId;
        ContributionCycleId = contributionCycleId;
        RecipientMemberId = recipientMemberId;
        Amount = amount;
        PaidAtUtc = DateTime.UtcNow;
    }
}