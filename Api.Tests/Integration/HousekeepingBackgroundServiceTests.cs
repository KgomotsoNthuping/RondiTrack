using System.Security.Cryptography;
using System.Text;
using Api.Background;
using Api.Services;
using Api.Tests.TestSupport;
using Domain.Exceptions;
using Domain.Models;
using Domain.Repositories;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Api.Tests;

[Collection("Postgres collection")]
public sealed class HousekeepingBackgroundServiceTests
{
    private readonly PostgresApiFactory _factory;

    public HousekeepingBackgroundServiceTests(
        PostgresApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task
        Refresh_token_inside_retention_survives_old_token_is_deleted_and_reuse_still_works()
    {
        var now =
            new DateTimeOffset(
                2099,
                1,
                31,
                12,
                0,
                0,
                TimeSpan.Zero);

        var rawRecentToken =
            $"recent-{Guid.NewGuid():N}";

        Guid recentTokenId;
        Guid oldTokenId;

        await using (
            var scope =
                _factory.Services
                    .CreateAsyncScope())
        {
            var db =
                scope.ServiceProvider
                    .GetRequiredService<
                        RondiTrackDbContext>();

            var unique =
                Guid.NewGuid()
                    .ToString("N");

            var user =
                new User(
                    $"Housekeeping {unique}",
                    $"housekeeping-{unique}@example.test",
                    "0824444444");

            user.SetPasswordHash(
                "test-only-password-hash");

            db.Users.Add(
                user);

            var recent =
                new RefreshToken(
                    user.Id,
                    HashToken(
                        rawRecentToken),
                    Guid.NewGuid(),
                    now.AddDays(-8)
                        .UtcDateTime,
                    now.AddDays(-1)
                        .UtcDateTime);

            // It was previously used.
            // Keeping this row is what allows
            // refresh-token replay detection.
            recent.Rotate(
                Guid.NewGuid(),
                now.AddDays(-2)
                    .UtcDateTime);

            var old =
                new RefreshToken(
                    user.Id,
                    HashToken(
                        $"old-{Guid.NewGuid():N}"),
                    Guid.NewGuid(),
                    now.AddDays(-50)
                        .UtcDateTime,
                    now.AddDays(-40)
                        .UtcDateTime);

            recentTokenId =
                recent.Id;

            oldTokenId =
                old.Id;

            db.RefreshTokens.AddRange(
                recent,
                old);

            await db.SaveChangesAsync();
        }

        var job =
            CreateJob(
                new FixedTimeProvider(
                    now));

        await job.RunOnceAsync();

        await using (
            var scope =
                _factory.Services
                    .CreateAsyncScope())
        {
            var db =
                scope.ServiceProvider
                    .GetRequiredService<
                        RondiTrackDbContext>();

            Assert.True(
                await db.RefreshTokens
                    .AnyAsync(
                        token =>
                            token.Id ==
                            recentTokenId));

            Assert.False(
                await db.RefreshTokens
                    .AnyAsync(
                        token =>
                            token.Id ==
                            oldTokenId));
        }

        await using var authScope =
            _factory.Services
                .CreateAsyncScope();

        var auth =
            authScope.ServiceProvider
                .GetRequiredService<
                    AuthService>();

        var exception =
            await Assert.ThrowsAsync<
                AuthenticationFailedException>(
                () =>
                    auth.RefreshAsync(
                        rawRecentToken));

        Assert.Contains(
            "already used",
            exception.Message,
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task
        Idempotency_record_inside_retention_still_replays()
    {
        var now =
            new DateTimeOffset(
                2099,
                1,
                31,
                12,
                0,
                0,
                TimeSpan.Zero);

        var key =
            $"inside-{Guid.NewGuid():N}";

        Guid contributionId;

        await using (
            var scope =
                _factory.Services
                    .CreateAsyncScope())
        {
            var db =
                scope.ServiceProvider
                    .GetRequiredService<
                        RondiTrackDbContext>();

            var scenario =
                await CreateContributionAsync(
                    db);

            contributionId =
                scenario.Contribution.Id;

            var repository =
                new EfIdempotencyRepository(
                    db,
                    new FixedTimeProvider(
                        now.AddHours(-24)));

            await repository.SaveAsync(
                key,
                new IdempotencyRecord(
                    new ContributionRequestSignature(
                        scenario.Stokvel.Id,
                        scenario.User.Id,
                        scenario.Cycle.Id,
                        scenario.Contribution.Amount),
                    scenario.Contribution));
        }

        var job =
            CreateJob(
                new FixedTimeProvider(
                    now));

        await job.RunOnceAsync();

        await using var replayScope =
            _factory.Services
                .CreateAsyncScope();

        var repositoryAfterCleanup =
            replayScope.ServiceProvider
                .GetRequiredService<
                    IIdempotencyRepository>();

        var replay =
            await repositoryAfterCleanup
                .GetAsync(key);

        Assert.NotNull(
            replay);

        Assert.Equal(
            contributionId,
            replay!.Contribution.Id);
    }

    [Fact]
    public async Task
        Old_idempotency_record_is_deleted_and_second_cleanup_is_safe()
    {
        var now =
            new DateTimeOffset(
                2099,
                1,
                31,
                12,
                0,
                0,
                TimeSpan.Zero);

        var key =
            $"old-{Guid.NewGuid():N}";

        await using (
            var scope =
                _factory.Services
                    .CreateAsyncScope())
        {
            var db =
                scope.ServiceProvider
                    .GetRequiredService<
                        RondiTrackDbContext>();

            var scenario =
                await CreateContributionAsync(
                    db);

            var repository =
                new EfIdempotencyRepository(
                    db,
                    new FixedTimeProvider(
                        now.AddHours(-49)));

            await repository.SaveAsync(
                key,
                new IdempotencyRecord(
                    new ContributionRequestSignature(
                        scenario.Stokvel.Id,
                        scenario.User.Id,
                        scenario.Cycle.Id,
                        scenario.Contribution.Amount),
                    scenario.Contribution));
        }

        var job =
            CreateJob(
                new FixedTimeProvider(
                    now));

        var first =
            await job.RunOnceAsync();

        Assert.True(
            first.IdempotencyRecordsDeleted >= 1);

        await using (
            var scope =
                _factory.Services
                    .CreateAsyncScope())
        {
            var repository =
                scope.ServiceProvider
                    .GetRequiredService<
                        IIdempotencyRepository>();

            Assert.Null(
                await repository
                    .GetAsync(key));
        }

        var second =
            await job.RunOnceAsync();

        Assert.Equal(
            0,
            second.IdempotencyRecordsDeleted);
    }

    private HousekeepingBackgroundService
        CreateJob(
            TimeProvider timeProvider)
    {
        return new HousekeepingBackgroundService(
            _factory.Services
                .GetRequiredService<
                    IServiceScopeFactory>(),
            timeProvider,
            new HousekeepingOptions(
                Interval:
                    TimeSpan.FromHours(1),
                RefreshTokenRetention:
                    TimeSpan.FromDays(30),
                IdempotencyRetention:
                    TimeSpan.FromHours(48)),
            _factory.Services
                .GetRequiredService<
                    ILogger<
                        HousekeepingBackgroundService>>());
    }

    private static async Task<
        ContributionScenario>
        CreateContributionAsync(
            RondiTrackDbContext db)
    {
        var unique =
            Guid.NewGuid()
                .ToString("N");

        var stokvel =
            new Stokvel(
                $"Idempotency {unique}",
                500m,
                1,
                12);

        var user =
            new User(
                $"Idempotency User {unique}",
                $"idempotency-{unique}@example.test",
                "0825555555");

        user.SetPasswordHash(
            "test-only-password-hash");

        var cycle =
            new ContributionCycle(
                stokvel.Id,
                1,
                500m,
                new DateOnly(
                    2099,
                    12,
                    25));

        var contribution =
            new Contribution(
                stokvel.Id,
                user.Id,
                cycle.Id,
                500m);

        db.Stokvels.Add(
            stokvel);

        db.Users.Add(
            user);

        db.StokvelMembers.Add(
            new StokvelMember(
                stokvel.Id,
                user.Id,
                StokvelMemberRole.Member,
                new DateTime(
                    2099,
                    1,
                    1,
                    0,
                    0,
                    0,
                    DateTimeKind.Utc)));

        db.ContributionCycles.Add(
            cycle);

        db.Contributions.Add(
            contribution);

        await db.SaveChangesAsync();

        return new ContributionScenario(
            stokvel,
            user,
            cycle,
            contribution);
    }

    private static string HashToken(
        string rawToken)
    {
        var bytes =
            Encoding.UTF8.GetBytes(
                rawToken);

        return Convert.ToHexString(
            SHA256.HashData(
                bytes));
    }

    private sealed class FixedTimeProvider
        : TimeProvider
    {
        private readonly DateTimeOffset
            _utcNow;

        public FixedTimeProvider(
            DateTimeOffset utcNow)
        {
            _utcNow = utcNow;
        }

        public override DateTimeOffset
            GetUtcNow()
        {
            return _utcNow;
        }
    }

    private sealed record ContributionScenario(
        Stokvel Stokvel,
        User User,
        ContributionCycle Cycle,
        Contribution Contribution);
}