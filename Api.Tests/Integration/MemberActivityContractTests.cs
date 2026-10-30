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
public sealed class MemberActivityContractTests
{
    private readonly PostgresApiFactory _factory;

    public MemberActivityContractTests(
        PostgresApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Feed_contains_all_three_activity_types()
    {
        var data = await TestData.CreateAsync(
            _factory.Services,
            memberCount: 3,
            createPayout: true);

        using var client =
            _factory.CreateAuthenticatedClient(
                data.UserIds.First());

        var items = await ReadAllAsync(
            client,
            pageSize: 2);

        Assert.Equal(7, items.Count);

        Assert.Contains(
            items,
            item => item.Type == "Contribution");

        Assert.Contains(
            items,
            item => item.Type == "Payout");

        Assert.Contains(
            items,
            item => item.Type == "MemberJoined");

        Assert.All(items, item =>
        {
            Assert.Equal(
                data.StokvelId,
                item.StokvelId);

            Assert.NotEqual(
                Guid.Empty,
                item.Id);

            Assert.False(
                string.IsNullOrWhiteSpace(
                    item.StokvelName));

            Assert.False(
                string.IsNullOrWhiteSpace(
                    item.MemberName));

            Assert.Equal(
                DateTimeKind.Utc,
                item.OccurredAtUtc.Kind);
        });

        Assert.All(
            items.Where(item =>
                item.Type == "MemberJoined"),
            item => Assert.Null(
                item.AmountInCents));

        Assert.All(
            items.Where(item =>
                item.Type != "MemberJoined"),
            item => Assert.True(
                item.AmountInCents > 0));

        var keys = items
            .Select(item =>
                $"{item.Type}:{item.StokvelId}:{item.Id}")
            .ToList();

        Assert.Equal(
            keys.Count,
            keys.Distinct().Count());

        var expectedOrder = items
            .OrderByDescending(item =>
                item.OccurredAtUtc)
            .ThenByDescending(item =>
                item.Type switch
                {
                    "Contribution" => 1,
                    "Payout" => 2,
                    _ => 3
                })
            .ThenByDescending(
                item => item.StokvelId.ToString("N"),
                StringComparer.Ordinal)
            .ThenByDescending(
                item => item.Id.ToString("N"),
                StringComparer.Ordinal)
            .Select(item =>
                $"{item.Type}:{item.StokvelId}:{item.Id}")
            .ToArray();

        Assert.Equal(
            expectedOrder,
            keys);
    }

    [Fact]
    public async Task Feed_contains_only_currently_joined_stokvels()
    {
        var first = await TestData.CreateAsync(
            _factory.Services,
            memberCount: 2);

        var second = await TestData.CreateAsync(
            _factory.Services,
            memberCount: 2,
            createPayout: true);

        using var client =
            _factory.CreateAuthenticatedClient(
                first.UserIds.First());

        var items = await ReadAllAsync(
            client,
            pageSize: 3);

        Assert.NotEmpty(items);

        Assert.All(
            items,
            item => Assert.Equal(
                first.StokvelId,
                item.StokvelId));

        Assert.DoesNotContain(
            items,
            item => item.StokvelId == second.StokvelId);
    }

    [Fact]
    public async Task Feed_combines_multiple_joined_stokvels()
    {
        var first = await TestData.CreateAsync(
            _factory.Services,
            memberCount: 2);

        var second = await TestData.CreateAsync(
            _factory.Services,
            memberCount: 2);

        var userId = first.UserIds.First();

        using (var scope =
            _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider
                .GetRequiredService<RondiTrackDbContext>();

            db.StokvelMembers.Add(
                new StokvelMember(
                    second.StokvelId,
                    userId));

            await db.SaveChangesAsync();
        }

        using var client =
            _factory.CreateAuthenticatedClient(userId);

        var items = await ReadAllAsync(
            client,
            pageSize: 3);

        Assert.Contains(
            items,
            item => item.StokvelId == first.StokvelId);

        Assert.Contains(
            items,
            item => item.StokvelId == second.StokvelId);

        var otherUserId = second.UserIds.First();

        using var otherClient =
            _factory.CreateAuthenticatedClient(
                otherUserId);

        var otherItems = await ReadAllAsync(
            otherClient,
            pageSize: 3);

        Assert.DoesNotContain(
            otherItems,
            item => item.StokvelId == first.StokvelId);
    }

    [Fact]
    public async Task Page_token_is_bound_to_user_and_page_size()
    {
        var data = await TestData.CreateAsync(
            _factory.Services,
            memberCount: 5);

        using var firstClient =
            _factory.CreateAuthenticatedClient(
                data.UserIds.First());

        var first = await firstClient.GetFromJsonAsync<
            PagedResponse<MemberActivityItemResponse>>(
                "/api/v1/me/activity?pageSize=2");

        Assert.NotNull(first);
        Assert.NotEmpty(first.NextPageToken);

        var token = Uri.EscapeDataString(
            first.NextPageToken);

        using var secondClient =
            _factory.CreateAuthenticatedClient(
                data.UserIds.Skip(1).First());

        var wrongUser = await secondClient.GetAsync(
            $"/api/v1/me/activity?pageSize=2&pageToken={token}");

        Assert.Equal(
            HttpStatusCode.BadRequest,
            wrongUser.StatusCode);

        var changedPageSize = await firstClient.GetAsync(
            $"/api/v1/me/activity?pageSize=3&pageToken={token}");

        Assert.Equal(
            HttpStatusCode.BadRequest,
            changedPageSize.StatusCode);
    }

    [Fact]
    public async Task Malformed_tokens_and_page_sizes_return_400()
    {
        var data = await TestData.CreateAsync(
            _factory.Services);

        using var client =
            _factory.CreateAuthenticatedClient(
                data.UserIds.First());

        var malformed = await client.GetAsync(
            "/api/v1/me/activity?pageToken=%40%40invalid");

        var zero = await client.GetAsync(
            "/api/v1/me/activity?pageSize=0");

        var excessive = await client.GetAsync(
            "/api/v1/me/activity?pageSize=101");

        Assert.Equal(
            HttpStatusCode.BadRequest,
            malformed.StatusCode);

        Assert.Equal(
            HttpStatusCode.BadRequest,
            zero.StatusCode);

        Assert.Equal(
            HttpStatusCode.BadRequest,
            excessive.StatusCode);
    }

    [Fact]
    public async Task Anonymous_request_receives_401()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync(
            "/api/v1/me/activity?pageSize=4");

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }

    [Fact]
    public async Task Feed_is_empty_for_user_without_memberships()
    {
        using var client =
            _factory.CreateAuthenticatedClient(
                Guid.NewGuid());

        var response = await client.GetFromJsonAsync<
            PagedResponse<MemberActivityItemResponse>>(
                "/api/v1/me/activity?pageSize=4");

        Assert.NotNull(response);
        Assert.Empty(response.Items);
        Assert.Empty(response.NextPageToken);
    }

    [Fact]
    public async Task Removing_membership_revokes_feed_visibility()
    {
        var data = await TestData.CreateAsync(
            _factory.Services,
            memberCount: 2,
            createContributions: false);

        var userId = data.UserIds.First();

        using var client =
            _factory.CreateAuthenticatedClient(userId);

        var before = await client.GetFromJsonAsync<
            PagedResponse<MemberActivityItemResponse>>(
                "/api/v1/me/activity");

        Assert.NotNull(before);
        Assert.NotEmpty(before.Items);

        using (var scope =
            _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider
                .GetRequiredService<RondiTrackDbContext>();

            var membership = await db.StokvelMembers
                .SingleAsync(member =>
                    member.StokvelId == data.StokvelId &&
                    member.UserId == userId);

            db.StokvelMembers.Remove(membership);

            await db.SaveChangesAsync();
        }

        var after = await client.GetFromJsonAsync<
            PagedResponse<MemberActivityItemResponse>>(
                "/api/v1/me/activity");

        Assert.NotNull(after);
        Assert.Empty(after.Items);
        Assert.Empty(after.NextPageToken);
    }
    
    [Fact]
    public async Task Identical_timestamps_do_not_skip_activity()
    {
        var data = await TestData.CreateAsync(
            _factory.Services,
            memberCount: 5,
            createContributions: false);

        var timestamp = new DateTime(
            2026, 10, 28, 10, 0, 0,
            DateTimeKind.Utc);

        using (var scope =
            _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider
                .GetRequiredService<RondiTrackDbContext>();

            var contributions = data.UserIds
                .Select(memberId =>
                    new Contribution(
                        data.StokvelId,
                        memberId,
                        data.CycleId,
                        500m,
                        ContributionStatus.Paid,
                        timestamp))
                .ToList();

            await db.Contributions.AddRangeAsync(
                contributions);

            await db.SaveChangesAsync();
        }

        using var client =
            _factory.CreateAuthenticatedClient(
                data.UserIds.First());

        var items = await ReadAllAsync(
            client,
            pageSize: 2);

        Assert.Equal(10, items.Count);

        Assert.Equal(
            5,
            items.Count(item =>
                item.Type == "Contribution"));

        var keys = items
            .Select(item =>
                $"{item.Type}:{item.StokvelId}:{item.Id}")
            .ToList();

        Assert.Equal(
            keys.Count,
            keys.Distinct().Count());
    }



    private static async Task<
        List<MemberActivityItemResponse>>
        ReadAllAsync(
            HttpClient client,
            int pageSize)
    {
        var items =
            new List<MemberActivityItemResponse>();

        var token = string.Empty;

        for (var pageNumber = 0;
             pageNumber < 50;
             pageNumber++)
        {
            var url =
                $"/api/v1/me/activity?pageSize={pageSize}";

            if (token.Length > 0)
            {
                url +=
                    "&pageToken=" +
                    Uri.EscapeDataString(token);
            }

            var response = await client.GetFromJsonAsync<
                PagedResponse<MemberActivityItemResponse>>(
                    url);

            Assert.NotNull(response);

            items.AddRange(response.Items);

            if (string.IsNullOrEmpty(
                response.NextPageToken))
            {
                return items;
            }

            token = response.NextPageToken;
        }

        throw new InvalidOperationException(
            "Activity feed did not reach its final page.");
    }
}
