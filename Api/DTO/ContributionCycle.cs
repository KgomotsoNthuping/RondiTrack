namespace Api.Contracts;

public sealed class CreateContributionCycleRequest
{
    public int CycleNumber { get; init; }

    public decimal TargetAmount { get; init; }
}

public sealed class UpdateContributionCycleRequest
{
    public int CycleNumber { get; init; }

    public decimal TargetAmount { get; init; }
}

public sealed record ContributionCycleResponse(
    Guid Id,
    Guid StokvelId,
    int CycleNumber,
    decimal TargetAmount,
    DateTime CreatedAtUtc);