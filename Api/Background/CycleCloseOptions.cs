namespace Api.Background;

public sealed record CycleCloseOptions(
    TimeSpan Interval);

public interface ICycleSchedule
{
    DateOnly GetLocalDate(
        DateTimeOffset utcNow);

    DateTimeOffset GetCloseInstantUtc(
        DateOnly endDate);

    DateOnly GetNextEndDate(
        DateOnly endDate);
}

public sealed class JohannesburgMonthlyCycleSchedule
    : ICycleSchedule
{
    private static readonly TimeZoneInfo TimeZone =
        TimeZoneInfo.FindSystemTimeZoneById(
            "Africa/Johannesburg");

    public DateOnly GetLocalDate(
        DateTimeOffset utcNow)
    {
        var local =
            TimeZoneInfo.ConvertTime(
                utcNow,
                TimeZone);

        return DateOnly.FromDateTime(
            local.DateTime);
    }

    public DateTimeOffset GetCloseInstantUtc(
        DateOnly endDate)
    {
        var nextDay =
            endDate.AddDays(1);

        var localClose =
            nextDay.ToDateTime(
                TimeOnly.MinValue,
                DateTimeKind.Unspecified);

        var utc =
            TimeZoneInfo.ConvertTimeToUtc(
                localClose,
                TimeZone);

        return new DateTimeOffset(utc);
    }

    public DateOnly GetNextEndDate(
        DateOnly endDate)
    {
        return endDate.AddMonths(1);
    }
}