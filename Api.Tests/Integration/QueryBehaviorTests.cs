using System.Net;
using System.Net.Http.Json;
using Api.DTO;
using Api.Models;
using Api.Tests.TestSupport;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Api.Tests;

public sealed class QueryBehaviorTests
    : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program>
        _factory;

    private readonly HttpClient _client;

    public QueryBehaviorTests(
        WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task
        ContributionFilter_ByAdminRole_ReturnsOnlyAdmin()
    {
        var data =
            await TestData.CreateAsync(
                _factory.Services,
                memberCount: 5);

        var response =
            await _client.GetFromJsonAsync<
                PagedResponse<
                    CycleContributionResponse>>(
                $"/api/stokvels/{data.StokvelId}" +
                $"/cycles/{data.CycleId}" +
                "/contributions?pageSize=20" +
                "&memberRole=Admin");

        Assert.NotNull(response);
        Assert.Single(response.Items);

        Assert.All(
            response.Items,
            contribution =>
                Assert.Equal(
                    StokvelMemberRole.Admin,
                    contribution.MemberRole));
    }

    [Fact]
    public async Task
        UnknownContributionSort_Returns400()
    {
        var data =
            await TestData.CreateAsync(
                _factory.Services);

        var response =
            await _client.GetAsync(
                $"/api/stokvels/{data.StokvelId}" +
                $"/cycles/{data.CycleId}" +
                "/contributions?sort=randomField");

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);
    }

    [Fact]
    public async Task
        MemberFullNameSort_ReturnsOrderedResults()
    {
        var data =
            await TestData.CreateAsync(
                _factory.Services,
                memberCount: 5);

        var response =
            await _client.GetFromJsonAsync<
                PagedResponse<UserResponse>>(
                $"/api/stokvels/{data.StokvelId}" +
                "/members?pageSize=20&sort=fullName");

        Assert.NotNull(response);

        var names =
            response.Items
                .Select(user => user.FullName)
                .ToList();

        var expected =
            names
                .OrderBy(name => name)
                .ToList();

        Assert.Equal(expected, names);
    }
}