using Api.Services;
using Api.Tests.TestSupport;
using Domain.Exceptions;
using Microsoft.Extensions.DependencyInjection;

namespace Api.Tests.Unit;

[Collection("Postgres collection")]
public sealed class TrackServiceTests
{
    private readonly PostgresApiFactory _factory;

    public TrackServiceTests(
        PostgresApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task
        AddMember_WhenMemberAlreadyExists_ThrowsDuplicateMembershipException()
    {
        var scenario =
            await TestData.CreateAsync(
                _factory.Services,
                memberCount: 1,
                createContributions: false);

        using var scope =
            _factory.Services.CreateScope();

        var service =
            scope.ServiceProvider
                .GetRequiredService<ITrackService>();

        var userId =
            scenario.UserIds.Single();

        async Task Action()
        {
            await service.AddMemberAsync(
                scenario.StokvelId,
                userId);
        }

        await Assert.ThrowsAsync<
            DuplicateMembershipException>(
                Action);
    }

    [Fact]
    public async Task
        RecordContribution_WhenSameMemberAlreadyPaidCycle_ThrowsDuplicateContributionException()
    {
        var scenario =
            await TestData.CreateAsync(
                _factory.Services,
                memberCount: 1,
                createContributions: false);

        using var scope =
            _factory.Services.CreateScope();

        var service =
            scope.ServiceProvider
                .GetRequiredService<ITrackService>();

        var userId =
            scenario.UserIds.Single();

        await service.RecordContributionAsync(
            scenario.StokvelId,
            userId,
            scenario.CycleId,
            500m,
            Guid.NewGuid().ToString("N"));

        async Task Action()
        {
            await service.RecordContributionAsync(
                scenario.StokvelId,
                userId,
                scenario.CycleId,
                500m,
                Guid.NewGuid().ToString("N"));
        }

        await Assert.ThrowsAsync<
            DuplicateContributionException>(
                Action);
    }

    [Fact]
    public async Task
        RecordContribution_WhenSameIdempotencyKeyAndPayloadAreRepeated_ReturnsOriginalContribution()
    {
        var scenario =
            await TestData.CreateAsync(
                _factory.Services,
                memberCount: 1,
                createContributions: false);

        using var scope =
            _factory.Services.CreateScope();

        var service =
            scope.ServiceProvider
                .GetRequiredService<ITrackService>();

        var userId =
            scenario.UserIds.Single();

        var key =
            Guid.NewGuid().ToString("N");

        var first =
            await service.RecordContributionAsync(
                scenario.StokvelId,
                userId,
                scenario.CycleId,
                500m,
                key);

        var second =
            await service.RecordContributionAsync(
                scenario.StokvelId,
                userId,
                scenario.CycleId,
                500m,
                key);

        Assert.Equal(
            first.Id,
            second.Id);

        Assert.Equal(
            first.Amount,
            second.Amount);
    }

    [Fact]
    public async Task
        RecordContribution_WhenSameKeyHasDifferentPayload_ThrowsIdempotencyConflictException()
    {
        var scenario =
            await TestData.CreateAsync(
                _factory.Services,
                memberCount: 1,
                createContributions: false);

        using var scope =
            _factory.Services.CreateScope();

        var service =
            scope.ServiceProvider
                .GetRequiredService<ITrackService>();

        var userId =
            scenario.UserIds.Single();

        var key =
            Guid.NewGuid().ToString("N");

        await service.RecordContributionAsync(
            scenario.StokvelId,
            userId,
            scenario.CycleId,
            500m,
            key);

        async Task Action()
        {
            await service.RecordContributionAsync(
                scenario.StokvelId,
                userId,
                scenario.CycleId,
                550m,
                key);
        }

        await Assert.ThrowsAsync<
            IdempotencyConflictException>(
                Action);
    }
}
