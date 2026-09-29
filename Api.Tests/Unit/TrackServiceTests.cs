using Api.Data;
using Api.Exceptions;
using Api.Services;

namespace Api.Tests.Unit;

public sealed class TrackServiceTests
{
    private static TrackService CreateService(
        out InMemoryTrackStore store)
    {
        store =
            new InMemoryTrackStore();

        var idempotencyStore =
            new InMemoryIdempotency();

        return new TrackService(
            store,
            idempotencyStore);
    }

    /// <summary>
    /// The service must reject an attempt to add someone who is already a member of that stokvel.
    /// </summary>
    [Fact]
    public async Task AddMember_WhenMemberAlreadyExists_ThrowsDuplicateMembershipException()
    {
        // Arrange
        var service =
            CreateService(out var store);

        var stokvel =
            (await store.GetStokvelsAsync())
                .First();

        var existingMember =
            stokvel.Members.First();

        // Act
        async Task Action()
        {
            await service.AddMemberAsync(
                stokvel.Id,
                existingMember.Id);
        }

        // Assert
        await Assert.ThrowsAsync<
            DuplicateMembershipException>(
                Action);
    }


    /// <summary>
    /// A member cannot have two contributions recorded for the same contribution cycle, even when a different idempotency key is used.
    /// </summary>
    [Fact]
    public async Task RecordContribution_WhenSameMemberAlreadyPaidCycle_ThrowsDuplicateContributionException()
    {
        // Arrange
        var service =
            CreateService(out var store);

        var stokvel =
            (await store.GetStokvelsAsync())
                .First();

        var member =
            stokvel.Members.First();

        var cycles =
            await store.GetContributionCyclesAsync(
                stokvel.Id);

        var cycle =
            cycles.First();

        // First contribution succeeds.
        await service.RecordContributionAsync(
            stokvel.Id,
            member.Id,
            cycle.Id,
            stokvel.MonthlyContribution,
            "first-key");

        // Act
        async Task Action()
        {
            await service.RecordContributionAsync(
                stokvel.Id,
                member.Id,
                cycle.Id,
                stokvel.MonthlyContribution,
                "second-key");
        }

        // Assert
        await Assert.ThrowsAsync<
            DuplicateContributionException>(
                Action);
    }

    /// <summary>
    /// Retrying the exact same contribution request with the same idempotency key must return the original contribution instead of creating another payment.
    /// </summary>
    [Fact]
    public async Task RecordContribution_WhenSameIdempotencyKeyAndPayloadAreRepeated_ReturnsOriginalContribution()
    {
        // Arrange
        var service =
            CreateService(out var store);

        var stokvel =
            (await store.GetStokvelsAsync())
                .First();

        var member =
            stokvel.Members.First();

        var cycle =
            (await store.GetContributionCyclesAsync(
                stokvel.Id))
                .First();

        const string key =
            "same-payment-key";

        // Act
        var first =
            await service.RecordContributionAsync(
                stokvel.Id,
                member.Id,
                cycle.Id,
                stokvel.MonthlyContribution,
                key);

        var second =
            await service.RecordContributionAsync(
                stokvel.Id,
                member.Id,
                cycle.Id,
                stokvel.MonthlyContribution,
                key);

        // Assert
        Assert.Equal(
            first.Id,
            second.Id);

        Assert.Equal(
            first.Amount,
            second.Amount);
    }

    /// <summary>
    /// The same idempotency key cannot be reused for a different contribution request. Changing the amount or other request data must cause a conflict.
    /// </summary>
    [Fact]
    public async Task RecordContribution_WhenSameKeyHasDifferentPayload_ThrowsIdempotencyConflictException()
    {
        // Arrange
        var service =
            CreateService(out var store);

        var stokvel =
            (await store.GetStokvelsAsync())
                .First();

        var member =
            stokvel.Members.First();

        var cycle =
            (await store.GetContributionCyclesAsync(
                stokvel.Id))
                .First();

        const string key =
            "payment-key";

        await service.RecordContributionAsync(
            stokvel.Id,
            member.Id,
            cycle.Id,
            stokvel.MonthlyContribution,
            key);

        // Act
        async Task Action()
        {
            await service.RecordContributionAsync(
                stokvel.Id,
                member.Id,
                cycle.Id,
                stokvel.MonthlyContribution + 50m,
                key);
        }

        // Assert
        await Assert.ThrowsAsync<
            IdempotencyConflictException>(
                Action);
    }
}