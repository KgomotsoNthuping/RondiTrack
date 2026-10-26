using System.Text.Json.Serialization;

namespace Domain.Models;

public sealed class Contribution
{
    public Guid Id { get; private set; }

    public Guid StokvelId { get; private set; }

    public Guid UserId { get; private set; }

    public Guid ContributionCycleId { get; private set; }

    public decimal Amount { get; private set; }
    
    public ContributionStatus Status { get; private set; }

    public DateTime? PaidAtUtc { get; private set; }

    [JsonIgnore]
    public StokvelMember Member { get; private set; } = null!;

    [JsonIgnore]
    public ContributionCycle ContributionCycle { get; private set; } = null!;

    public Contribution(Guid stokvelId, Guid userId, Guid contributionCycleId, decimal amount, ContributionStatus status = ContributionStatus.Paid,
    DateTime? paidAtUtc = null)
    {
        if (stokvelId == Guid.Empty)
        {
            throw new ArgumentException("A stokvel ID is required.");
        }

        if (userId == Guid.Empty)
        {
            throw new ArgumentException("A user ID is required.");
        }

        if (contributionCycleId == Guid.Empty)
        {
            throw new ArgumentException("A contribution cycle ID is required.");
        }

        if (amount <= 0)
        {
            throw new ArgumentException("The contribution amount must be greater than zero.");
        }

        Id = Guid.NewGuid();
        StokvelId = stokvelId;
        UserId = userId;
        ContributionCycleId = contributionCycleId;
        Amount = amount;

        if (!Enum.IsDefined(status))
        {
            throw new ArgumentException("Invalid contribution status.", nameof(status));
        }

        if (status == ContributionStatus.Pending && paidAtUtc.HasValue)
        {
            throw new ArgumentException("Pending contributions cannot have a payment date.");
        }

        DateTime? paymentTime = status == ContributionStatus.Pending ? null : paidAtUtc ?? DateTime.UtcNow;

        if (paymentTime.HasValue && paymentTime.Value.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException("The payment date must be in UTC.");
        }

        Status = status;
        PaidAtUtc = paymentTime;

    }
}