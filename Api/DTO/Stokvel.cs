namespace RondiTrack.Api.Contracts;

// Information the client is allowed to send when creating a Stokvel.
public sealed class CreateStokvelRequest
{
    public string? Name { get; init; }

    public decimal MonthlyContribution { get; init; }

    public int CurrentPeriod { get; init; }

    public int TotalPeriods { get; init; }

    public string? Rules { get; init; }
}

// Information the client is allowed to update.
public sealed class UpdateStokvelRequest
{
    public string? Name { get; init; }

    public decimal MonthlyContribution { get; init; }

    public int CurrentPeriod { get; init; }

    public int TotalPeriods { get; init; }

    public string? Rules { get; init; }
}

public sealed record StokvelResponse(
    Guid Id,
    string Name,
    decimal MonthlyContribution,
    int CurrentPeriod,
    int TotalPeriods,
    string? Rules,
    int MemberCount);