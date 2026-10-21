using Api.Background;
using Api.Observability;
using Api.Tests.TestSupport;
using Domain.Models;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Api.Tests;

[Collection("Postgres collection")]
public sealed class CycleCloseBackgroundServiceTests : IAsyncLifetime
{
    private readonly PostgresApiFactory _factory;

    // The collection shares a test database, so each test must remove the
    // future-dated cycles it created before the next test starts.
    private readonly List<Guid> _seededStokvelIds = new();
    private readonly List<Guid> _seededUserIds = new();

    public CycleCloseBackgroundServiceTests(
        PostgresApiFactory factory)
    {
        _factory = factory;
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        if (_seededStokvelIds.Count == 0)
        {
            return;
        }

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider
            .GetRequiredService<RondiTrackDbContext>();

        // Delete dependents before principals to satisfy PostgreSQL FKs.
        // Only rows owned by this individual test instance are removed.
        await db.IdempotencyEntries
            .Where(entry => _seededStokvelIds.Contains(entry.StokvelId))
            .ExecuteDeleteAsync();
        await db.Arrears
            .Where(arrear => _seededStokvelIds.Contains(arrear.StokvelId))
            .ExecuteDeleteAsync();
        await db.Payouts
            .Where(payout => _seededStokvelIds.Contains(payout.StokvelId))
            .ExecuteDeleteAsync();
        await db.Contributions
            .Where(contribution => _seededStokvelIds.Contains(contribution.StokvelId))
            .ExecuteDeleteAsync();
        await db.ContributionCycles
            .Where(cycle => _seededStokvelIds.Contains(cycle.StokvelId))
            .ExecuteDeleteAsync();
        await db.StokvelMembers
            .Where(member => _seededStokvelIds.Contains(member.StokvelId))
            .ExecuteDeleteAsync();
        await db.Stokvels
            .Where(stokvel => _seededStokvelIds.Contains(stokvel.Id))
            .ExecuteDeleteAsync();
        await db.Users
            .Where(user => _seededUserIds.Contains(user.Id))
            .ExecuteDeleteAsync();
    }

    [Fact]
    public async Task
        Ended_cycle_closes_records_exact_arrears_and_opens_next_cycle()
    {
        var scenario =
            await SeedTwoMemberCycleAsync(
                new DateOnly(
                    2099,
                    1,
                    25));

        var clock =
            new FixedTimeProvider(
                new DateTimeOffset(
                    2099,
                    1,
                    25,
                    22,
                    0,
                    0,
                    TimeSpan.Zero));

        var job =
            CreateJob(clock);

        var result =
            await job.RunOnceAsync();

        Assert.Equal(
            1,
            result.CyclesClosed);

        Assert.Equal(
            1,
            result.ArrearsRecorded);

        await using var scope =
            _factory.Services
                .CreateAsyncScope();

        var db =
            scope.ServiceProvider
                .GetRequiredService<
                    RondiTrackDbContext>();

        var closedCycle =
            await db.ContributionCycles
                .AsNoTracking()
                .SingleAsync(
                    cycle =>
                        cycle.Id ==
                        scenario.CycleId);

        Assert.NotNull(
            closedCycle.ClosedAtUtc);

        Assert.Equal(
            new DateTime(
                2099,
                1,
                25,
                22,
                0,
                0,
                DateTimeKind.Utc),
            closedCycle
                .ClosedAtUtc!
                .Value);

        var arrears =
            await db.Arrears
                .AsNoTracking()
                .Where(
                    arrear =>
                        arrear
                            .ContributionCycleId ==
                        scenario.CycleId)
                .ToListAsync();

        var arrear =
            Assert.Single(
                arrears);

        Assert.Equal(
            scenario.UnderpaidUserId,
            arrear.UserId);

        Assert.Equal(
            20_000L,
            arrear.ShortfallInCents);

        Assert.DoesNotContain(
            arrears,
            value =>
                value.UserId ==
                scenario.PaidUserId);

        var nextCycle =
            await db.ContributionCycles
                .AsNoTracking()
                .SingleAsync(
                    cycle =>
                        cycle.StokvelId ==
                            scenario.StokvelId &&
                        cycle.CycleNumber == 2);

        Assert.Equal(
            new DateOnly(
                2099,
                2,
                25),
            nextCycle.EndDate!.Value);

        Assert.Null(
            nextCycle.ClosedAtUtc);
    }

    [Fact]
    public async Task
        Cycle_ending_tomorrow_is_not_touched()
    {
        var scenario =
            await SeedTwoMemberCycleAsync(
                new DateOnly(
                    2000,
                    1,
                    26));

        var clock =
            new FixedTimeProvider(
                new DateTimeOffset(
                    2000,
                    1,
                    25,
                    10,
                    0,
                    0,
                    TimeSpan.Zero));

        var job =
            CreateJob(clock);

        var result =
            await job.RunOnceAsync();

        Assert.Equal(
            0,
            result.CyclesClosed);

        await using var scope =
            _factory.Services
                .CreateAsyncScope();

        var db =
            scope.ServiceProvider
                .GetRequiredService<
                    RondiTrackDbContext>();

        var cycle =
            await db.ContributionCycles
                .AsNoTracking()
                .SingleAsync(
                    value =>
                        value.Id ==
                        scenario.CycleId);

        Assert.Null(
            cycle.ClosedAtUtc);

        Assert.Equal(
            1,
            await db.ContributionCycles
                .CountAsync(
                    value =>
                        value.StokvelId ==
                        scenario.StokvelId));

        Assert.Equal(
            0,
            await db.Arrears
                .CountAsync(
                    value =>
                        value.StokvelId ==
                        scenario.StokvelId));
    }

    [Fact]
    public async Task
        Boundary_2359_stays_open_and_midnight_closes()
    {
        var scenario =
            await SeedTwoMemberCycleAsync(
                new DateOnly(
                    2099,
                    1,
                    25));

        // 23:59 SAST on 25 January.
        var clock =
            new FixedTimeProvider(
                new DateTimeOffset(
                    2099,
                    1,
                    25,
                    21,
                    59,
                    0,
                    TimeSpan.Zero));

        var job =
            CreateJob(clock);

        var before =
            await job.RunOnceAsync();

        Assert.Equal(
            0,
            before.CyclesClosed);

        await using (
            var scope =
                _factory.Services
                    .CreateAsyncScope())
        {
            var db =
                scope.ServiceProvider
                    .GetRequiredService<
                        RondiTrackDbContext>();

            var stillOpen =
                await db.ContributionCycles
                    .AsNoTracking()
                    .SingleAsync(
                        value =>
                            value.Id ==
                            scenario.CycleId);

            Assert.Null(
                stillOpen.ClosedAtUtc);
        }

        // 00:00 SAST on 26 January.
        clock.SetUtcNow(
            new DateTimeOffset(
                2099,
                1,
                25,
                22,
                0,
                0,
                TimeSpan.Zero));

        var atMidnight =
            await job.RunOnceAsync();

        Assert.Equal(
            1,
            atMidnight.CyclesClosed);

        await using var finalScope =
            _factory.Services
                .CreateAsyncScope();

        var finalDb =
            finalScope.ServiceProvider
                .GetRequiredService<
                    RondiTrackDbContext>();

        var closed =
            await finalDb
                .ContributionCycles
                .AsNoTracking()
                .SingleAsync(
                    value =>
                        value.Id ==
                        scenario.CycleId);

        Assert.NotNull(
            closed.ClosedAtUtc);
    }

    [Fact]
    public async Task
        Running_twice_does_not_change_database_state()
    {
        var scenario =
            await SeedTwoMemberCycleAsync(
                new DateOnly(
                    2099,
                    1,
                    25));

        var clock =
            new FixedTimeProvider(
                new DateTimeOffset(
                    2099,
                    1,
                    25,
                    22,
                    0,
                    0,
                    TimeSpan.Zero));

        var job =
            CreateJob(clock);

        var first =
            await job.RunOnceAsync();

        await using (
            var firstScope =
                _factory.Services
                    .CreateAsyncScope())
        {
            var db =
                firstScope.ServiceProvider
                    .GetRequiredService<
                        RondiTrackDbContext>();

            var cycleCount =
                await db.ContributionCycles
                    .CountAsync(
                        value =>
                            value.StokvelId ==
                            scenario.StokvelId);

            var arrearCount =
                await db.Arrears
                    .CountAsync(
                        value =>
                            value.StokvelId ==
                            scenario.StokvelId);

            Assert.Equal(
                2,
                cycleCount);

            Assert.Equal(
                1,
                arrearCount);
        }

        var second =
            await job.RunOnceAsync();

        Assert.Equal(
            1,
            first.CyclesClosed);

        Assert.Equal(
            0,
            second.CyclesClosed);

        Assert.Equal(
            0,
            second.ArrearsRecorded);

        await using var secondScope =
            _factory.Services
                .CreateAsyncScope();

        var finalDb =
            secondScope.ServiceProvider
                .GetRequiredService<
                    RondiTrackDbContext>();

        Assert.Equal(
            2,
            await finalDb
                .ContributionCycles
                .CountAsync(
                    value =>
                        value.StokvelId ==
                        scenario.StokvelId));

        Assert.Equal(
            1,
            await finalDb.Arrears
                .CountAsync(
                    value =>
                        value.StokvelId ==
                        scenario.StokvelId));
    }

    [Fact]
    public async Task
        Concurrent_runs_close_once_and_record_arrears_once()
    {
        var scenario =
            await SeedTwoMemberCycleAsync(
                new DateOnly(
                    2099,
                    1,
                    25));

        var now =
            new DateTimeOffset(
                2099,
                1,
                25,
                22,
                0,
                0,
                TimeSpan.Zero);

        var firstJob =
            CreateJob(
                new FixedTimeProvider(
                    now));

        var secondJob =
            CreateJob(
                new FixedTimeProvider(
                    now));

        await Task.WhenAll(
            firstJob.RunOnceAsync(),
            secondJob.RunOnceAsync());

        await using var scope =
            _factory.Services
                .CreateAsyncScope();

        var db =
            scope.ServiceProvider
                .GetRequiredService<
                    RondiTrackDbContext>();

        var original =
            await db.ContributionCycles
                .AsNoTracking()
                .SingleAsync(
                    value =>
                        value.Id ==
                        scenario.CycleId);

        Assert.NotNull(
            original.ClosedAtUtc);

        Assert.Equal(
            1,
            await db.Arrears
                .CountAsync(
                    value =>
                        value
                            .ContributionCycleId ==
                        scenario.CycleId));

        Assert.Equal(
            1,
            await db.ContributionCycles
                .CountAsync(
                    value =>
                        value.StokvelId ==
                            scenario.StokvelId &&
                        value.CycleNumber == 2));
    }

    [Fact]
    public async Task
        Catch_up_closes_three_missed_cycles_and_second_run_is_safe()
    {
        var scenario =
            await SeedSingleMemberCycleAsync(
                new DateOnly(
                    2098,
                    11,
                    25));

        var clock =
            new FixedTimeProvider(
                new DateTimeOffset(
                    2099,
                    1,
                    25,
                    22,
                    0,
                    0,
                    TimeSpan.Zero));

        var job =
            CreateJob(clock);

        var first =
            await job.RunOnceAsync();

        Assert.Equal(
            3,
            first.CyclesClosed);

        Assert.Equal(
            3,
            first.ArrearsRecorded);

        await using (
            var scope =
                _factory.Services
                    .CreateAsyncScope())
        {
            var db =
                scope.ServiceProvider
                    .GetRequiredService<
                        RondiTrackDbContext>();

            Assert.Equal(
                3,
                await db.ContributionCycles
                    .CountAsync(
                        value =>
                            value.StokvelId ==
                                scenario.StokvelId &&
                            value.ClosedAtUtc != null));

            Assert.Equal(
                3,
                await db.Arrears
                    .CountAsync(
                        value =>
                            value.StokvelId ==
                            scenario.StokvelId));

            var next =
                await db.ContributionCycles
                    .AsNoTracking()
                    .SingleAsync(
                        value =>
                            value.StokvelId ==
                                scenario.StokvelId &&
                            value.CycleNumber == 4);

            Assert.Equal(
                new DateOnly(
                    2099,
                    2,
                    25),
                next.EndDate!.Value);

            Assert.Null(
                next.ClosedAtUtc);
        }

        var second =
            await job.RunOnceAsync();

        Assert.Equal(
            0,
            second.CyclesClosed);

        Assert.Equal(
            0,
            second.ArrearsRecorded);
    }

    [Fact]
    public async Task
        Invalid_stokvel_does_not_stop_valid_stokvel()
    {
        var invalid =
            await SeedTwoMemberCycleAsync(
                new DateOnly(
                    2099,
                    1,
                    25));

        var valid =
            await SeedTwoMemberCycleAsync(
                new DateOnly(
                    2099,
                    1,
                    25));

        await using (
            var scope =
                _factory.Services
                    .CreateAsyncScope())
        {
            var db =
                scope.ServiceProvider
                    .GetRequiredService<
                        RondiTrackDbContext>();

            await db.Database
                .ExecuteSqlInterpolatedAsync(
                    $"""
                    UPDATE "Stokvels"
                    SET "TotalPeriods" = 0
                    WHERE "Id" = {invalid.StokvelId}
                    """);
        }

        var job =
            CreateJob(
                new FixedTimeProvider(
                    new DateTimeOffset(
                        2099,
                        1,
                        25,
                        22,
                        0,
                        0,
                        TimeSpan.Zero)));

        var exception =
            await Record.ExceptionAsync(
                () =>
                    job.RunOnceAsync());

        // This test deliberately set TotalPeriods to 0. Restore the value
        // so scheduled background jobs in other tests do not retry it forever.
        await using (var cleanupScope = _factory.Services.CreateAsyncScope())
        {
            var cleanupDb = cleanupScope.ServiceProvider
                .GetRequiredService<RondiTrackDbContext>();
            await cleanupDb.Database.ExecuteSqlInterpolatedAsync(
                $"""UPDATE "Stokvels" SET "TotalPeriods" = 12 WHERE "Id" = {invalid.StokvelId}""");
        }

        Assert.Null(
            exception);

        await using var finalScope =
            _factory.Services
                .CreateAsyncScope();

        var finalDb =
            finalScope.ServiceProvider
                .GetRequiredService<
                    RondiTrackDbContext>();

        var invalidCycle =
            await finalDb
                .ContributionCycles
                .AsNoTracking()
                .SingleAsync(
                    value =>
                        value.Id ==
                        invalid.CycleId);

        var validCycle =
            await finalDb
                .ContributionCycles
                .AsNoTracking()
                .SingleAsync(
                    value =>
                        value.Id ==
                        valid.CycleId);

        Assert.Null(
            invalidCycle.ClosedAtUtc);

        Assert.NotNull(
            validCycle.ClosedAtUtc);
    }

    private CycleCloseBackgroundService
        CreateJob(
            TimeProvider timeProvider)
    {
        return new CycleCloseBackgroundService(
            _factory.Services
                .GetRequiredService<
                    IServiceScopeFactory>(),
            timeProvider,
            new JohannesburgMonthlyCycleSchedule(),
            _factory.Services
                .GetRequiredService<
                    CycleCloseOptions>(),
            _factory.Services
                .GetRequiredService<
                    CycleCloseMetrics>(),
            _factory.Services
                .GetRequiredService<
                    ILogger<
                        CycleCloseBackgroundService>>());
    }

    private async Task<TwoMemberScenario>
        SeedTwoMemberCycleAsync(
            DateOnly endDate)
    {
        await using var scope =
            _factory.Services
                .CreateAsyncScope();

        var db =
            scope.ServiceProvider
                .GetRequiredService<
                    RondiTrackDbContext>();

        var unique =
            Guid.NewGuid()
                .ToString("N");

        var stokvel =
            new Stokvel(
                $"Cycle Job {unique}",
                500m,
                1,
                12);

        var underpaid =
            new User(
                $"Underpaid {unique}",
                $"underpaid-{unique}@example.test",
                "0821111111");

        var paid =
            new User(
                $"Paid {unique}",
                $"paid-{unique}@example.test",
                "0822222222");

        underpaid.SetPasswordHash(
            "test-only-password-hash");

        paid.SetPasswordHash(
            "test-only-password-hash");

        var cycle =
            new ContributionCycle(
                stokvel.Id,
                1,
                1000m,
                endDate);

        var joinedAt =
            new DateTime(
                2098,
                1,
                1,
                0,
                0,
                0,
                DateTimeKind.Utc);

        db.Stokvels.Add(
            stokvel);

        db.Users.AddRange(
            underpaid,
            paid);

        db.StokvelMembers.AddRange(
            new StokvelMember(
                stokvel.Id,
                underpaid.Id,
                StokvelMemberRole.Member,
                joinedAt),

            new StokvelMember(
                stokvel.Id,
                paid.Id,
                StokvelMemberRole.Member,
                joinedAt));

        db.ContributionCycles.Add(
            cycle);

        db.Contributions.AddRange(
            new Contribution(
                stokvel.Id,
                underpaid.Id,
                cycle.Id,
                300m),

            new Contribution(
                stokvel.Id,
                paid.Id,
                cycle.Id,
                500m));

        await db.SaveChangesAsync();

        _seededStokvelIds.Add(stokvel.Id);
        _seededUserIds.Add(underpaid.Id);
        _seededUserIds.Add(paid.Id);

        return new TwoMemberScenario(
            stokvel.Id,
            cycle.Id,
            underpaid.Id,
            paid.Id);
    }

    private async Task<SingleMemberScenario>
        SeedSingleMemberCycleAsync(
            DateOnly endDate)
    {
        await using var scope =
            _factory.Services
                .CreateAsyncScope();

        var db =
            scope.ServiceProvider
                .GetRequiredService<
                    RondiTrackDbContext>();

        var unique =
            Guid.NewGuid()
                .ToString("N");

        var stokvel =
            new Stokvel(
                $"Catch Up {unique}",
                500m,
                1,
                12);

        var member =
            new User(
                $"Member {unique}",
                $"member-{unique}@example.test",
                "0823333333");

        member.SetPasswordHash(
            "test-only-password-hash");

        var cycle =
            new ContributionCycle(
                stokvel.Id,
                1,
                500m,
                endDate);

        db.Stokvels.Add(
            stokvel);

        db.Users.Add(
            member);

        db.StokvelMembers.Add(
            new StokvelMember(
                stokvel.Id,
                member.Id,
                StokvelMemberRole.Member,
                new DateTime(
                    2098,
                    1,
                    1,
                    0,
                    0,
                    0,
                    DateTimeKind.Utc)));

        db.ContributionCycles.Add(
            cycle);

        await db.SaveChangesAsync();

        _seededStokvelIds.Add(stokvel.Id);
        _seededUserIds.Add(member.Id);

        return new SingleMemberScenario(
            stokvel.Id,
            cycle.Id);
    }

    private sealed class FixedTimeProvider
        : TimeProvider
    {
        private DateTimeOffset _utcNow;

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

        public void SetUtcNow(
            DateTimeOffset utcNow)
        {
            _utcNow = utcNow;
        }
    }

    private sealed record TwoMemberScenario(
        Guid StokvelId,
        Guid CycleId,
        Guid UnderpaidUserId,
        Guid PaidUserId);

    private sealed record SingleMemberScenario(
        Guid StokvelId,
        Guid CycleId);
}