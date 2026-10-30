
using System.Net;
using System.Net.Http.Json;
using Api.DTO;
using Api.Tests.TestSupport;
using Domain.Models;
using Infrastructure.Data;
using Microsoft.Extensions.DependencyInjection;

namespace Api.Tests.Integration;

[Collection("Postgres collection")]
public sealed class ContributionHistoryContractTests
{
    private readonly PostgresApiFactory _factory;

    public ContributionHistoryContractTests(
        PostgresApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task History_pages_newest_first_without_duplicates()
    {
        var data = await TestData.CreateAsync(
            _factory.Services,
            memberCount: 5);

        using var client =
            _factory.CreateAuthenticatedClient(
                data.UserIds.First());

        var url =
            $"/api/v1/stokvels/{data.StokvelId}" +
            "/contributions?pageSize=2";

        var first = await client.GetFromJsonAsync<
            PagedResponse<ContributionListItemResponse>>(
                url);

        Assert.NotNull(first);
        Assert.Equal(2, first.Items.Count);
        Assert.NotEmpty(first.NextPageToken);

        var second = await client.GetFromJsonAsync<
            PagedResponse<ContributionListItemResponse>>(
                url +
                "&pageToken=" +
                Uri.EscapeDataString(first.NextPageToken));

        Assert.NotNull(second);
        Assert.Equal(2, second.Items.Count);

        var third = await client.GetFromJsonAsync<
            PagedResponse<ContributionListItemResponse>>(
                url +
                "&pageToken=" +
                Uri.EscapeDataString(second.NextPageToken));

        Assert.NotNull(third);
        Assert.Single(third.Items);
        Assert.Empty(third.NextPageToken);

        var items = first.Items
            .Concat(second.Items)
            .Concat(third.Items)
            .ToList();

        Assert.Equal(5, items.Count);
        Assert.Equal(
            5,
            items.Select(item => item.Id)
                .Distinct()
                .Count());

        var times = items
            .Select(item => item.PaidAtUtc)
            .ToList();

        Assert.Equal(
            times.OrderByDescending(time => time),
            times);
    }

    [Fact]
    public async Task Member_filter_returns_only_selected_member()
    {
        var data = await TestData.CreateAsync(
            _factory.Services,
            memberCount: 4);

        using var client =
            _factory.CreateAuthenticatedClient(
                data.UserIds.First());

        var selectedMember = data.UserIds.Skip(1).First();

        var result = await client.GetFromJsonAsync<
            PagedResponse<ContributionListItemResponse>>(
                $"/api/v1/stokvels/{data.StokvelId}" +
                $"/contributions?member={selectedMember}");

        Assert.NotNull(result);
        Assert.Single(result.Items);

        Assert.All(
            result.Items,
            item => Assert.Equal(
                selectedMember,
                item.UserId));
    }

    [Fact]
    public async Task Token_cannot_be_reused_with_another_member()
    {
        var data = await TestData.CreateAsync(
            _factory.Services,
            memberCount: 5);

        using var client =
            _factory.CreateAuthenticatedClient(
                data.UserIds.First());

        var url =
            $"/api/v1/stokvels/{data.StokvelId}" +
            "/contributions?pageSize=2";

        var first = await client.GetFromJsonAsync<
            PagedResponse<ContributionListItemResponse>>(
                url);

        Assert.NotNull(first);
        Assert.NotEmpty(first.NextPageToken);

        var otherMember = data.UserIds.Skip(1).First();

        var response = await client.GetAsync(
            url +
            $"&member={otherMember}" +
            "&pageToken=" +
            Uri.EscapeDataString(first.NextPageToken));

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);

        var body = await response.Content
            .ReadAsStringAsync();

        Assert.Contains(
            "pageToken",
            body,
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Invalid_member_and_date_return_400()
    {
        var data = await TestData.CreateAsync(
            _factory.Services);

        using var client =
            _factory.CreateAuthenticatedClient(
                data.UserIds.First());

        var baseUrl =
            $"/api/v1/stokvels/{data.StokvelId}" +
            "/contributions";

        var invalidMember = await client.GetAsync(
            baseUrl + "?member=banana");

        var unknownMember = await client.GetAsync(
            baseUrl + $"?member={Guid.NewGuid()}");

        var invalidDate = await client.GetAsync(
            baseUrl +
            "?from=2026-02-30T00:00:00Z");

        var reversedRange = await client.GetAsync(
            baseUrl +
            "?from=2026-11-01T00:00:00Z" +
            "&to=2026-10-01T00:00:00Z");

        Assert.Equal(
            HttpStatusCode.BadRequest,
            invalidMember.StatusCode);

        Assert.Equal(
            HttpStatusCode.BadRequest,
            unknownMember.StatusCode);

        Assert.Equal(
            HttpStatusCode.BadRequest,
            invalidDate.StatusCode);

        Assert.Equal(
            HttpStatusCode.BadRequest,
            reversedRange.StatusCode);
    }

    [Fact]
    public async Task October_range_is_inclusive_at_start_and_exclusive_at_end()
    {
        var data = await TestData.CreateAsync(
            _factory.Services,
            memberCount: 4,
            createContributions: false);

        var memberIds = data.UserIds.ToArray();

        var times = new[]
        {
            new DateTime(
                2026, 9, 30, 21, 59, 59,
                DateTimeKind.Utc),
            new DateTime(
                2026, 9, 30, 22, 0, 0,
                DateTimeKind.Utc),
            new DateTime(
                2026, 10, 31, 21, 30, 0,
                DateTimeKind.Utc),
            new DateTime(
                2026, 10, 31, 22, 0, 0,
                DateTimeKind.Utc)
        };

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider
                .GetRequiredService<RondiTrackDbContext>();

            var contributions = times
                .Select((time, index) =>
                    new Contribution(
                        data.StokvelId,
                        memberIds[index],
                        data.CycleId,
                        500m,
                        ContributionStatus.Paid,
                        time))
                .ToList();

            await db.Contributions
                .AddRangeAsync(contributions);

            await db.SaveChangesAsync();
        }

        using var client =
            _factory.CreateAuthenticatedClient(
                memberIds[0]);

        var result = await client.GetFromJsonAsync<
            PagedResponse<ContributionListItemResponse>>(
                $"/api/v1/stokvels/{data.StokvelId}" +
                "/contributions" +
                "?from=2026-09-30T22:00:00Z" +
                "&to=2026-10-31T22:00:00Z");

        Assert.NotNull(result);
        Assert.Equal(2, result.Items.Count);

        var returnedTimes = result.Items
            .Select(item => item.PaidAtUtc)
            .ToArray();

        Assert.Contains(times[1], returnedTimes);
        Assert.Contains(times[2], returnedTimes);
        Assert.DoesNotContain(times[0], returnedTimes);
        Assert.DoesNotContain(times[3], returnedTimes);
    }
}
