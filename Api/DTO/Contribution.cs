namespace Api.DTO;

// Client send recording of the payment they make
public sealed class RecordContributionRequest
{
    public Guid UserId { get; init; } //maybe use private set

    public int CycleNumber { get; init; }

    public decimal Amount { get; init; }
}

//API sends info back after payment made
public sealed record ContributionResponse(
    Guid Id,
    Guid StokvelId,
    Guid UserId,
    int CycleNumber,
    decimal Amount);