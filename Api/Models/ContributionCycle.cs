using System.Text.Json.Serialization;

namespace Api.Models;

public sealed class ContributionCycle
{
    public Guid Id { get; private set; }

    public Guid StokvelId { get; private set; }

    public int CycleNumber { get; private set; }

    public decimal TargetAmount { get; private set; }

    public ContributionCycleStatus Status { get; private set; }

    [JsonIgnore]
    public ICollection<Contribution> Contributions { get; private set; } = new List<Contribution>();

    public ContributionCycle(
        Guid stokvelId,
        int cycleNumber,
        decimal targetAmount)
    {
        if (stokvelId == Guid.Empty)
        {
            throw new ArgumentException("A stokvel ID is required.");
        }

        if (cycleNumber <= 0)
        {
            throw new ArgumentException("Cycle number must be greater than zero.");
        }

        if (targetAmount <= 0)
        {
            throw new ArgumentException("Target amount must be greater than zero.");
        }

        Id = Guid.NewGuid();
        StokvelId = stokvelId;
        CycleNumber = cycleNumber;
        TargetAmount = targetAmount;
        Status = ContributionCycleStatus.Open;
    }

    public void Update(
        int cycleNumber,
        decimal targetAmount)
    {
        if (cycleNumber <= 0)
        {
            throw new ArgumentException("Cycle number must be greater than zero.");
        }

        if (targetAmount <= 0)
        {
            throw new ArgumentException("Target amount must be greater than zero.");
        }
        CycleNumber = cycleNumber;
        TargetAmount = targetAmount;
    }

    public void MarkPaidOut()
    {
        if (Status == ContributionCycleStatus.PaidOut)
        {
            throw new InvalidOperationException("The contribution cycle has already been paid out.");
        }
        Status = ContributionCycleStatus.PaidOut;
    }
}