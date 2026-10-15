using System.Net;
using System.Net.Http.Json;
using Api.DTO;
using Api.Tests.TestSupport;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Api.Tests;

[Collection("Postgres collection")]
public sealed class PaginationTests
{
    private readonly PostgresApiFactory _factory;

    public PaginationTests(PostgresApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task
        ContributionPages_ReturnAllRowsWithoutDuplicates()
    {
        var data =
            await TestData.CreateAsync(
                _factory.Services,
                memberCount: 5);

        using var client = _factory.CreateAuthenticatedClient(
            data.UserIds.First());

        var first =
            await client.GetFromJsonAsync<
                PagedResponse<
                    CycleContributionResponse>>(
                $"/api/v1/stokvels/{data.StokvelId}" +
                $"/cycles/{data.CycleId}" +
                "/contributions?pageSize=2");

        Assert.NotNull(first);
        Assert.Equal(2, first.Items.Count);
        Assert.False(
            string.IsNullOrWhiteSpace(
                first.NextPageToken));

        var second =
            await client.GetFromJsonAsync<
                PagedResponse<
                    CycleContributionResponse>>(
                $"/api/v1/stokvels/{data.StokvelId}" +
                $"/cycles/{data.CycleId}" +
                "/contributions?pageSize=2" +
                $"&pageToken={Uri.EscapeDataString(first.NextPageToken)}");

        Assert.NotNull(second);
        Assert.Equal(2, second.Items.Count);

        var third =
            await client.GetFromJsonAsync<
                PagedResponse<
                    CycleContributionResponse>>(
                $"/api/v1/stokvels/{data.StokvelId}" +
                $"/cycles/{data.CycleId}" +
                "/contributions?pageSize=2" +
                $"&pageToken={Uri.EscapeDataString(second.NextPageToken)}");

        Assert.NotNull(third);
        Assert.Single(third.Items);
        Assert.Equal(
            string.Empty,
            third.NextPageToken);

        var allIds =
            first.Items
                .Concat(second.Items)
                .Concat(third.Items)
                .Select(item => item.Id)
                .ToList();

        Assert.Equal(5, allIds.Count);
        Assert.Equal(
            5,
            allIds.Distinct().Count());
    }

    [Fact]
    public async Task
        MembersEndpoint_IsAlsoPaged()
    {
        var data =
            await TestData.CreateAsync(
                _factory.Services,
                memberCount: 5);

        using var client = _factory.CreateAuthenticatedClient(
            data.UserIds.First());

        var response =
            await client.GetFromJsonAsync<
                PagedResponse<UserResponse>>(
                $"/api/v1/stokvels/{data.StokvelId}" +
                "/members?pageSize=2");

        Assert.NotNull(response);
        Assert.Equal(2, response.Items.Count);
        Assert.False(
            string.IsNullOrWhiteSpace(
                response.NextPageToken));
    }

    [Fact]
    public async Task
        NegativePageSize_Returns400()
    {
        var data =
            await TestData.CreateAsync(
                _factory.Services);

        using var client = _factory.CreateAuthenticatedClient(
            data.UserIds.First());

        var response =
            await client.GetAsync(
                $"/api/v1/stokvels/{data.StokvelId}" +
                $"/cycles/{data.CycleId}" +
                "/contributions?pageSize=-1");

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);

        Assert.Contains(
            "application/problem+json",
            response.Content.Headers
                .ContentType?
                .MediaType);
    }

    [Fact]
    public async Task
        TokenCannotBeReusedWithDifferentSort()
    {
        var data =
            await TestData.CreateAsync(
                _factory.Services,
                memberCount: 5);

        using var client = _factory.CreateAuthenticatedClient(
            data.UserIds.First());

        var first =
            await client.GetFromJsonAsync<
                PagedResponse<
                    CycleContributionResponse>>(
                $"/api/v1/stokvels/{data.StokvelId}" +
                $"/cycles/{data.CycleId}" +
                "/contributions?pageSize=2&sort=userId");

        Assert.NotNull(first);

        var response =
            await client.GetAsync(
                $"/api/v1/stokvels/{data.StokvelId}" +
                $"/cycles/{data.CycleId}" +
                "/contributions?pageSize=2" +
                "&sort=amount" +
                $"&pageToken={Uri.EscapeDataString(first.NextPageToken)}");

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);
    }
}
