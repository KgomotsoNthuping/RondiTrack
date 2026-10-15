using System.Net;
using System.Net.Http.Json;
using Api.DTO;
using Api.Tests.TestSupport;
using Domain.Models;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Api.Tests.Integration;

[Collection("Postgres collection")]
public sealed class EdgeCaseTests
{
    private readonly PostgresApiFactory _factory;

    public EdgeCaseTests(PostgresApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetMembers_WhenStokvelHasNoMembers_RequiresMembership()
    {
        // With the current membership policy, a caller who has no membership
        // must not receive the members list, even when the list is empty.
        var data = await TestData.CreateAsync(
            _factory.Services,
            memberCount: 1,
            createContributions: false);

        var emptyStokvelId = await CreateEmptyStokvelAsync();
        using var client = _factory.CreateAuthenticatedClient(data.UserIds.First());

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<RondiTrackDbContext>();
        Assert.False(await db.StokvelMembers.AnyAsync(
            m => m.StokvelId == emptyStokvelId));

        using var response = await client.GetAsync(
            $"/api/v1/stokvels/{emptyStokvelId}/members");

        // The membership policy intentionally blocks a non-member.
        Assert.Contains(response.StatusCode, new[]
        {
            HttpStatusCode.Forbidden,
            HttpStatusCode.NotFound
        });
    }

    [Fact]
    public async Task CreateContributionCycle_WithCycleNumberOne_Returns201()
    {
        var data = await TestData.CreateAsync(
            _factory.Services,
            memberCount: 1,
            createContributions: false);

        // A new stokvel has no cycle #1; the seeded stokvel already does.
        var stokvelId = await CreateEmptyStokvelAsync();
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<RondiTrackDbContext>();
            db.StokvelMembers.Add(new StokvelMember(
                stokvelId,
                data.UserIds.First(),
                StokvelMemberRole.Admin));
            await db.SaveChangesAsync();
        }

        using var client = _factory.CreateAuthenticatedClient(data.UserIds.First());
        using var response = await client.PostAsJsonAsync(
            $"/api/v1/stokvels/{stokvelId}/cycles",
            new CreateContributionCycleRequest
            {
                CycleNumber = 1,
                TargetAmount = 1000m
            });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var cycle = await response.Content.ReadFromJsonAsync<ContributionCycleResponse>();
        Assert.NotNull(cycle);
        Assert.Equal(1, cycle.CycleNumber);
        Assert.Equal(1000m, cycle.TargetAmount);
    }

    [Fact]
    public async Task RecordContribution_WhenUserExistsButIsNotMember_Returns422ProblemJson()
    {
        // Admin has access to the stokvel but the target contributor does not.
        // This reaches the business rule without bypassing authorization.
        var data = await TestData.CreateAsync(
            _factory.Services,
            memberCount: 1,
            createContributions: false);

        using var client = _factory.CreateAuthenticatedClient(data.UserIds.First());

        // Create an existing user without registering them in this stokvel.
        // The test concerns contribution membership, not user registration.
        var nonMember = new User(
            "Non Member Test User",
            $"nonmember-{Guid.NewGuid():N}@example.co.za",
            "0791234567");

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider
                .GetRequiredService<RondiTrackDbContext>();

            db.Users.Add(nonMember);
            await db.SaveChangesAsync();
        }

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"/api/v1/stokvels/{data.StokvelId}/contributions");
        request.Headers.Add("Idempotency-Key", $"nonmember-{Guid.NewGuid():N}");
        request.Content = JsonContent.Create(new RecordContributionRequest
        {
            UserId = nonMember.Id,
            ContributionCycleId = data.CycleId,
            Amount = 500m
        });

        using var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("\"status\":422", body);
        Assert.Contains("correlationId", body);
    }

    private async Task<Guid> CreateEmptyStokvelAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<RondiTrackDbContext>();
        var stokvel = new Stokvel(
            $"Empty Boundary Stokvel {Guid.NewGuid():N}",
            500m,
            1,
            12);
        db.Stokvels.Add(stokvel);
        await db.SaveChangesAsync();
        return stokvel.Id;
    }
}
