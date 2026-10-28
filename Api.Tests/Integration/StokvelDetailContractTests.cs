
using System.Net;
using System.Net.Http.Json;
using Api.DTO;
using Api.Tests.TestSupport;
using Domain.Models;
using Infrastructure.Data;
using Microsoft.Extensions.DependencyInjection;

namespace Api.Tests.Integration;

[Collection("Postgres collection")]
public sealed class StokvelDetailContractTests
{
    private readonly PostgresApiFactory _factory;

    public StokvelDetailContractTests(
        PostgresApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Detail_returns_member_role_and_cycle_summary()
    {
        var data = await TestData.CreateAsync(
            _factory.Services,
            memberCount: 2,
            createContributions: false);

        var userId = data.UserIds.First();

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider
                .GetRequiredService<RondiTrackDbContext>();

            db.Contributions.AddRange(
                new Contribution(
                    data.StokvelId,
                    data.UserIds.First(),
                    data.CycleId,
                    500m,
                    ContributionStatus.Paid),
                new Contribution(
                    data.StokvelId,
                    data.UserIds.Last(),
                    data.CycleId,
                    500m,
                    ContributionStatus.Pending));

            await db.SaveChangesAsync();
        }

        using var client =
            _factory.CreateAuthenticatedClient(userId);

        var response = await client.GetAsync(
            $"/api/v1/stokvels/{data.StokvelId}");

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var detail = await response.Content
            .ReadFromJsonAsync<StokvelDetailResponse>();

        Assert.NotNull(detail);
        Assert.Equal(data.StokvelId, detail.Id);
        Assert.Equal("Admin", detail.Role);
        Assert.Equal(2, detail.MemberCount);
        Assert.Equal(50000L, detail.MonthlyContributionInCents);
        Assert.Equal(1, detail.CurrentPeriod);

        Assert.NotNull(detail.CurrentCycle);
        Assert.Equal(data.CycleId, detail.CurrentCycle.Id);
        Assert.Equal("Open", detail.CurrentCycle.Status);
        Assert.Equal(500000L, detail.CurrentCycle.TargetAmountInCents);
        Assert.Equal(50000L, detail.CurrentCycle.TotalCollectedInCents);
        Assert.Equal(450000L, detail.CurrentCycle.AmountOutstandingInCents);
    }

    [Fact]
    public async Task Detail_does_not_reveal_other_stokvels()
    {
        var data = await TestData.CreateAsync(
            _factory.Services,
            memberCount: 1,
            createContributions: false);

        using var client =
            _factory.CreateAuthenticatedClient(Guid.NewGuid());

        var otherMembersStokvel = await client.GetAsync(
            $"/api/v1/stokvels/{data.StokvelId}");

        var nonexistentStokvel = await client.GetAsync(
            $"/api/v1/stokvels/{Guid.NewGuid()}");

        var malformedId = await client.GetAsync(
            "/api/v1/stokvels/not-a-guid");

        Assert.Equal(
            HttpStatusCode.NotFound,
            otherMembersStokvel.StatusCode);

        Assert.Equal(
            HttpStatusCode.NotFound,
            nonexistentStokvel.StatusCode);

        Assert.Equal(
            HttpStatusCode.NotFound,
            malformedId.StatusCode);
    }

    [Fact]
    public async Task Member_home_returns_empty_list_for_new_user()
    {
        var user = new User(
            "Independent Member",
            $"independent-{Guid.NewGuid():N}@ronditrack.test",
            "0821234567");

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider
                .GetRequiredService<RondiTrackDbContext>();

            db.Users.Add(user);

            await db.SaveChangesAsync();
        }

        using var client =
            _factory.CreateAuthenticatedClient(user.Id);

        var response = await client.GetAsync(
            "/api/v1/me/home");

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var home = await response.Content
            .ReadFromJsonAsync<MemberHomeResponse>();

        Assert.NotNull(home);
        Assert.Empty(home.Stokvels);
    }
}
