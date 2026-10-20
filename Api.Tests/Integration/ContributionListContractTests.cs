
using System.Net;
using System.Net.Http.Json;
using Api.DTO;
using Api.Tests.TestSupport;

namespace Api.Tests.Integration;

[Collection("Postgres collection")]
public sealed class ContributionListContractTests
{
    private readonly PostgresApiFactory _factory;

    public ContributionListContractTests(
        PostgresApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task List_returns_cents_status_date_and_pages()
    {
        var data = await TestData.CreateAsync(
            _factory.Services,
            memberCount: 3);

        using var client =
            _factory.CreateAuthenticatedClient(
                data.UserIds.First());

        var url =
            $"/api/v1/stokvels/{data.StokvelId}" +
            "/contributions?pageSize=2";

        var firstResponse = await client.GetAsync(url);

        Assert.Equal(
            HttpStatusCode.OK,
            firstResponse.StatusCode);

        var first = await firstResponse.Content
            .ReadFromJsonAsync<
                PagedResponse<ContributionListItemResponse>>();

        Assert.NotNull(first);
        Assert.Equal(2, first.Items.Count);
        Assert.NotEmpty(first.NextPageToken);

        Assert.All(first.Items, item =>
        {
            Assert.Equal("Paid", item.Status);
            Assert.Equal(1, item.CycleNumber);
            Assert.NotNull(item.PaidAtUtc);
            Assert.Equal(
                DateTimeKind.Utc,
                item.PaidAtUtc!.Value.Kind);

            Assert.True(item.AmountInCents >= 50000);
        });

        var second = await client.GetFromJsonAsync<
            PagedResponse<ContributionListItemResponse>>(
                url + "&pageToken=" + first.NextPageToken);

        Assert.NotNull(second);
        Assert.Single(second.Items);
        Assert.Equal(
            string.Empty,
            second.NextPageToken);

        var allIds = first.Items
            .Concat(second.Items)
            .Select(item => item.Id)
            .ToList();

        Assert.Equal(3, allIds.Distinct().Count());
    }
}
