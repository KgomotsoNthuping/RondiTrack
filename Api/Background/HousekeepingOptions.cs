namespace Api.Background;

public sealed record HousekeepingOptions(
    TimeSpan Interval,
    TimeSpan RefreshTokenRetention,
    TimeSpan IdempotencyRetention);