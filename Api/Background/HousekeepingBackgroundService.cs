using Domain.Repositories;

namespace Api.Background;

public sealed class
    HousekeepingBackgroundService
        : BackgroundService
{
    private readonly IServiceScopeFactory
        _scopeFactory;

    private readonly TimeProvider
        _timeProvider;

    private readonly HousekeepingOptions
        _options;

    private readonly ILogger<
        HousekeepingBackgroundService>
        _logger;

    public HousekeepingBackgroundService(
        IServiceScopeFactory scopeFactory,
        TimeProvider timeProvider,
        HousekeepingOptions options,
        ILogger<
            HousekeepingBackgroundService>
            logger)
    {
        _scopeFactory = scopeFactory;
        _timeProvider = timeProvider;
        _options = options;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        using var timer =
            new PeriodicTimer(
                _options.Interval,
                _timeProvider);

        try
        {
            do
            {
                await RunOnceAsync(
                    stoppingToken);
            }
            while (
                await timer.WaitForNextTickAsync(
                    stoppingToken));
        }
        catch (OperationCanceledException)
            when (
                stoppingToken
                    .IsCancellationRequested)
        {
            // Normal shutdown.
        }
    }

    public async Task<
        HousekeepingRunResult>
        RunOnceAsync(
            CancellationToken
                cancellationToken = default)
    {
        try
        {
            var now =
                _timeProvider
                    .GetUtcNow()
                    .UtcDateTime;

            var refreshCutoff =
                now -
                _options
                    .RefreshTokenRetention;

            var idempotencyCutoff =
                now -
                _options
                    .IdempotencyRetention;

            await using var scope =
                _scopeFactory
                    .CreateAsyncScope();

            var refreshTokens =
                scope.ServiceProvider
                    .GetRequiredService<
                        IRefreshTokenRepository>();

            var idempotency =
                scope.ServiceProvider
                    .GetRequiredService<
                        IIdempotencyRepository>();

            var deletedRefreshTokens =
                await refreshTokens
                    .DeleteExpiredBeforeAsync(
                        refreshCutoff,
                        cancellationToken);

            var deletedIdempotency =
                await idempotency
                    .DeleteCreatedBeforeAsync(
                        idempotencyCutoff,
                        cancellationToken);

            _logger.LogInformation(
                "Housekeeping deleted {RefreshTokenCount} refresh tokens and {IdempotencyCount} idempotency records.",
                deletedRefreshTokens,
                deletedIdempotency);

            return new HousekeepingRunResult(
                deletedRefreshTokens,
                deletedIdempotency);
        }
        catch (Exception exception)
            when (
                exception
                    is not
                    OperationCanceledException)
        {
            _logger.LogError(
                exception,
                "Housekeeping failed; it will retry on the next interval.");

            return new HousekeepingRunResult(
                0,
                0);
        }
    }
}

public sealed record HousekeepingRunResult(
    int RefreshTokensDeleted,
    int IdempotencyRecordsDeleted);