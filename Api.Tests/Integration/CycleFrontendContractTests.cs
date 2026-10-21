using System.Net;
using System.Net.Http.Json;
using Api.DTO;
using Api.Tests.TestSupport;
using Domain.Models;
using Infrastructure.Data;
using Microsoft.Extensions.DependencyInjection;

namespace Api.Tests.Integration;

[Collection("Postgres collection")]
public sealed class CycleFrontendContractTests
{
    private readonly PostgresApiFactory _factory;

    public CycleFrontendContractTests(PostgresApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Cycles_are_newest_first_with_server_totals_and_due_dates()
    {
        var data = await TestData.CreateAsync(
            _factory.Services,
            memberCount: 3,
            createContributions: false);

        var members = data.UserIds.ToList();
        Guid secondCycleId;

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<RondiTrackDbContext>();
            var secondCycle = new ContributionCycle(
                data.StokvelId, 2, 7500m, new DateOnly(2026, 11, 30));
            secondCycleId = secondCycle.Id;

            db.ContributionCycles.Add(secondCycle);
            db.Contributions.AddRange(
                new Contribution(data.StokvelId, members[0], data.CycleId,
                    500m, ContributionStatus.Paid),
                new Contribution(data.StokvelId, members[1], data.CycleId,
                    200m, ContributionStatus.Confirmed),
                new Contribution(data.StokvelId, members[2], data.CycleId,
                    1000m, ContributionStatus.Pending));
            await db.SaveChangesAsync();
        }

        using var client = _factory.CreateAuthenticatedClient(members[0]);
        var url = $"/api/v1/stokvels/{data.StokvelId}/cycles";
        var response = await client.GetAsync(url);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var cycles = await response.Content.ReadFromJsonAsync<List<ContributionCycleResponse>>();
        Assert.NotNull(cycles);
        Assert.Equal(2, cycles.Count);
        Assert.Equal(secondCycleId, cycles[0].Id);
        Assert.Equal(data.CycleId, cycles[1].Id);
        Assert.Equal("Open", cycles[0].Status);
        Assert.Equal(new DateOnly(2026, 11, 30), cycles[0].DueDate);
        Assert.Equal(0L, cycles[0].TotalCollectedInCents);
        Assert.Equal(750000L, cycles[0].AmountDueInCents);
        Assert.Equal(70000L, cycles[1].TotalCollectedInCents);
        Assert.Equal(500000L, cycles[1].AmountDueInCents);

        var single = await client.GetFromJsonAsync<ContributionCycleResponse>(
            $"{url}/{data.CycleId}");
        Assert.NotNull(single);
        Assert.Equal(70000L, single.TotalCollectedInCents);
    }

    [Fact]
    public async Task Member_home_returns_only_the_authenticated_members_arrears()
    {
        var data = await TestData.CreateAsync(
            _factory.Services, memberCount: 2, createContributions: false);
        var members = data.UserIds.ToList();

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<RondiTrackDbContext>();
            var now = DateTime.UtcNow;
            db.Arrears.AddRange(
                new Arrear(data.StokvelId, members[0], data.CycleId, 12500L, now),
                new Arrear(data.StokvelId, members[1], data.CycleId, 30000L, now));
            await db.SaveChangesAsync();
        }

        using var firstClient = _factory.CreateAuthenticatedClient(members[0]);
        using var secondClient = _factory.CreateAuthenticatedClient(members[1]);

        var firstHome = await firstClient.GetFromJsonAsync<MemberHomeResponse>(
            "/api/v1/me/home");
        var secondHome = await secondClient.GetFromJsonAsync<MemberHomeResponse>(
            "/api/v1/me/home");

        Assert.NotNull(firstHome);
        Assert.NotNull(secondHome);
        Assert.Equal(12500L, firstHome.Stokvels.Single(
            item => item.StokvelId == data.StokvelId).ArrearsInCents);
        Assert.Equal(30000L, secondHome.Stokvels.Single(
            item => item.StokvelId == data.StokvelId).ArrearsInCents);
    }
}
