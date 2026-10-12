using System.Net;
using System.Net.Http.Json;
using Infrastructure.Data;
using Api.DTO;
using Domain.Models;
using Api.Tests.TestSupport;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Api.Tests.TestSupport;

namespace Api.Tests;

[Collection("Postgres collection")]
public sealed class ConcurrencyTests
{
    private readonly PostgresApiFactory _factory;

    private readonly HttpClient _client;

    public ConcurrencyTests(PostgresApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task
        TwoContextsEditingSamePayout_ThrowsConcurrencyException()
    {
        var data =
            await TestData.CreateAsync(
                _factory.Services,
                memberCount: 2,
                createPayout: true);

        // Two independent contexts are necessary because
        // each context must remember its own original xmin.
        using var scopeOne =
            _factory.Services.CreateScope();

        using var scopeTwo =
            _factory.Services.CreateScope();

        var contextOne =
            scopeOne.ServiceProvider
                .GetRequiredService<
                    RondiTrackDbContext>();

        var contextTwo =
            scopeTwo.ServiceProvider
                .GetRequiredService<
                    RondiTrackDbContext>();

        var first =
            await contextOne.Payouts
                .SingleAsync(payout =>
                    payout.Id ==
                        data.PayoutId);

        var second =
            await contextTwo.Payouts
                .SingleAsync(payout =>
                    payout.Id ==
                        data.PayoutId);

        contextOne.Entry(first)
            .Property(payout =>
                payout.Amount)
            .CurrentValue += 10m;

        await contextOne
            .SaveChangesAsync();

        contextTwo.Entry(second)
            .Property(payout =>
                payout.Amount)
            .CurrentValue += 20m;

        await Assert.ThrowsAsync<
            DbUpdateConcurrencyException>(
                () =>
                    contextTwo
                        .SaveChangesAsync());
    }

    [Fact]
    public async Task
        ReusingStaleCycleVersion_Returns409()
    {
        var data =
            await TestData.CreateAsync(
                _factory.Services,
                memberCount: 1);

        var current =
            await _client.GetFromJsonAsync<
                ContributionCycleResponse>(
                $"/api/stokvels/{data.StokvelId}" +
                $"/cycles/{data.CycleId}");

        Assert.NotNull(current);

        var firstUpdate =
            new UpdateContributionCycleRequest
            {
                CycleNumber =
                    current.CycleNumber,

                TargetAmount =
                    current.TargetAmount + 100m,

                Version =
                    current.Version
            };

        var firstResponse =
            await _client.PutAsJsonAsync(
                $"/api/stokvels/{data.StokvelId}" +
                $"/cycles/{data.CycleId}",
                firstUpdate);

        Assert.Equal(
            HttpStatusCode.OK,
            firstResponse.StatusCode);

        var updated =
            await firstResponse.Content
                .ReadFromJsonAsync<
                    ContributionCycleResponse>();

        Assert.NotNull(updated);

        Assert.NotEqual(
            current.Version,
            updated.Version);

        // Send the original version again.
        var staleUpdate =
            new UpdateContributionCycleRequest
            {
                CycleNumber =
                    current.CycleNumber,

                TargetAmount =
                    current.TargetAmount + 200m,

                Version =
                    current.Version
            };

        var staleResponse =
            await _client.PutAsJsonAsync(
                $"/api/stokvels/{data.StokvelId}" +
                $"/cycles/{data.CycleId}",
                staleUpdate);

        Assert.Equal(
            HttpStatusCode.Conflict,
            staleResponse.StatusCode);

        Assert.Contains(
            "application/problem+json",
            staleResponse.Content.Headers
                .ContentType?
                .MediaType);
    }
}