namespace Api.Models;

public sealed class Contribution
{
    public Guid Id { get; private set; }

    public Guid StokvelId { get; private set; }

    public Guid UserId { get; private set; }

    public Guid ContributionCycleId { get; private set; }

    public decimal Amount { get; private set; }

    public Contribution(Guid stokvelId, Guid userId, Guid contributionCycleId, decimal amount)
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
    }
}