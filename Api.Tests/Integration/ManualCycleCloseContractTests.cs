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
public sealed class ManualCycleCloseContractTests
{
    private readonly PostgresApiFactory _factory;

    public ManualCycleCloseContractTests(
        PostgresApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Preview_returns_member_names_and_exact_arrears()
    {
        var (data, treasurerId) = await CreateScenarioAsync();

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider
                .GetRequiredService<RondiTrackDbContext>();

            db.Contributions.AddRange(
                new Contribution(
                    data.StokvelId,
                    data.UserIds.First(),
                    data.CycleId,
                    300m),
                new Contribution(
                    data.StokvelId,
                    data.UserIds.Last(),
                    data.CycleId,
                    500m));

            await db.SaveChangesAsync();
        }

        using var treasurer =
            _factory.CreateAuthenticatedClient(treasurerId);

        var preview = await treasurer
            .GetFromJsonAsync<CycleClosePreviewResponse>(
                PreviewUrl(data.StokvelId, data.CycleId));

        Assert.NotNull(preview);
        Assert.Equal(64, preview.Version.Length);
        Assert.Equal(2, preview.Arrears.Count);
        Assert.Equal(70_000L, preview.TotalArrearsInCents);

        Assert.Contains(
            preview.Arrears,
            item =>
                item.UserId == data.UserIds.First() &&
                item.ShortfallInCents == 20_000L &&
                !string.IsNullOrWhiteSpace(item.MemberName));

        Assert.Contains(
            preview.Arrears,
            item =>
                item.UserId == treasurerId &&
                item.ShortfallInCents == 50_000L);

        Assert.DoesNotContain(
            preview.Arrears,
            item => item.UserId == data.UserIds.Last());
    }

    

    [Fact]
    public async Task Stale_preview_returns_409_after_contribution()
    {
        var (data, treasurerId) = await CreateScenarioAsync();

        using var treasurer =
            _factory.CreateAuthenticatedClient(treasurerId);

        using var member =
            _factory.CreateAuthenticatedClient(
                data.UserIds.First());

        var original = await treasurer
            .GetFromJsonAsync<CycleClosePreviewResponse>(
                PreviewUrl(data.StokvelId, data.CycleId));

        Assert.NotNull(original);
        Assert.Equal(
            150_000L,
            original.TotalArrearsInCents);

        using var recording = new HttpRequestMessage(
            HttpMethod.Post,
            $"/api/v1/stokvels/{data.StokvelId}/cycles/{data.CycleId}/contributions");

        recording.Headers.Add(
            "Idempotency-Key",
            Guid.NewGuid().ToString("N"));

        recording.Content = JsonContent.Create(new
        {
            UserId = data.UserIds.First(),
            AmountInCents = 50_000L
        });

        using var recorded =
            await member.SendAsync(recording);

        Assert.Equal(
            HttpStatusCode.Created,
            recorded.StatusCode);

        var closeKey = Guid.NewGuid().ToString("N");

        using var stale = await CloseAsync(
            treasurer,
            data.StokvelId,
            data.CycleId,
            original.Version,
            closeKey);

        Assert.Equal(
            HttpStatusCode.Conflict,
            stale.StatusCode);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider
                .GetRequiredService<RondiTrackDbContext>();

            var cycle = await db.ContributionCycles
                .AsNoTracking()
                .SingleAsync(item => item.Id == data.CycleId);

            Assert.Null(cycle.ClosedAtUtc);

            Assert.Equal(
                0,
                await db.Arrears.CountAsync(
                    item => item.ContributionCycleId == data.CycleId));
        }

        var fresh = await treasurer
            .GetFromJsonAsync<CycleClosePreviewResponse>(
                PreviewUrl(data.StokvelId, data.CycleId));

        Assert.NotNull(fresh);
        Assert.NotEqual(original.Version, fresh.Version);
        Assert.Equal(
            100_000L,
            fresh.TotalArrearsInCents);

        using var accepted = await CloseAsync(
            treasurer,
            data.StokvelId,
            data.CycleId,
            fresh.Version,
            closeKey);

        Assert.Equal(
            HttpStatusCode.OK,
            accepted.StatusCode);

        var result = await accepted.Content
            .ReadFromJsonAsync<CloseCycleResponse>();

        Assert.NotNull(result);
        Assert.Equal(2, result.ArrearsCount);
        Assert.Equal(
            100_000L,
            result.TotalArrearsInCents);
    }

    [Fact]
    public async Task Same_key_replays_original_close_without_new_arrears()
    {
        var (data, treasurerId) = await CreateScenarioAsync();

        using var treasurer =
            _factory.CreateAuthenticatedClient(treasurerId);

        var preview = await treasurer
            .GetFromJsonAsync<CycleClosePreviewResponse>(
                PreviewUrl(data.StokvelId, data.CycleId));

        Assert.NotNull(preview);

        var key = Guid.NewGuid().ToString("N");

        using var first = await CloseAsync(
            treasurer,
            data.StokvelId,
            data.CycleId,
            preview.Version,
            key);

        using var second = await CloseAsync(
            treasurer,
            data.StokvelId,
            data.CycleId,
            preview.Version,
            key);

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(HttpStatusCode.OK, second.StatusCode);

        Assert.Equal(
            await first.Content.ReadAsStringAsync(),
            await second.Content.ReadAsStringAsync());

        using var newKey = await CloseAsync(
            treasurer,
            data.StokvelId,
            data.CycleId,
            preview.Version,
            Guid.NewGuid().ToString("N"));

        Assert.Equal(
            HttpStatusCode.Conflict,
            newKey.StatusCode);

        using var changedRequest = await CloseAsync(
            treasurer,
            data.StokvelId,
            data.CycleId,
            new string('A', 64),
            key);

        Assert.Equal(
            HttpStatusCode.Conflict,
            changedRequest.StatusCode);

        using var scope = _factory.Services.CreateScope();

        var db = scope.ServiceProvider
            .GetRequiredService<RondiTrackDbContext>();

        Assert.Equal(
            3,
            await db.Arrears.CountAsync(
                item => item.ContributionCycleId == data.CycleId));

        Assert.Equal(
            1,
            await db.CycleCloseReceipts.CountAsync(
                item => item.CycleId == data.CycleId));

        Assert.Equal(
            2,
            await db.ContributionCycles.CountAsync(
                item => item.StokvelId == data.StokvelId));
    }

    [Fact]
    public async Task Only_treasurer_of_this_stokvel_can_close()
    {
        var (data, treasurerId) = await CreateScenarioAsync();

        var (other, otherTreasurerId) =
            await CreateScenarioAsync();

        using var treasurer =
            _factory.CreateAuthenticatedClient(treasurerId);

        using var ordinaryMember =
            _factory.CreateAuthenticatedClient(
                data.UserIds.Last());

        using var admin =
            _factory.CreateAuthenticatedClient(
                data.UserIds.First());

        using var otherTreasurer =
            _factory.CreateAuthenticatedClient(
                otherTreasurerId);

        using var anonymous = _factory.CreateClient();

        var preview = await treasurer
            .GetFromJsonAsync<CycleClosePreviewResponse>(
                PreviewUrl(data.StokvelId, data.CycleId));

        Assert.NotNull(preview);

        var url = PreviewUrl(data.StokvelId, data.CycleId);

        using var memberPreview =
            await ordinaryMember.GetAsync(url);

        using var adminPreview =
            await admin.GetAsync(url);

        using var otherPreview =
            await otherTreasurer.GetAsync(url);

        using var anonymousPreview =
            await anonymous.GetAsync(url);

        Assert.Equal(
            HttpStatusCode.Forbidden,
            memberPreview.StatusCode);

        Assert.Equal(
            HttpStatusCode.Forbidden,
            adminPreview.StatusCode);

        Assert.Equal(
            HttpStatusCode.Forbidden,
            otherPreview.StatusCode);

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            anonymousPreview.StatusCode);

        using var memberClose = await CloseAsync(
            ordinaryMember,
            data.StokvelId,
            data.CycleId,
            preview.Version,
            Guid.NewGuid().ToString("N"));

        using var otherClose = await CloseAsync(
            otherTreasurer,
            data.StokvelId,
            data.CycleId,
            preview.Version,
            Guid.NewGuid().ToString("N"));

        Assert.Equal(
            HttpStatusCode.Forbidden,
            memberClose.StatusCode);

        Assert.Equal(
            HttpStatusCode.Forbidden,
            otherClose.StatusCode);
    }

    [Fact]
    public async Task Closed_cycle_refuses_new_contributions()
    {
        var (data, treasurerId) = await CreateScenarioAsync();

        using var treasurer =
            _factory.CreateAuthenticatedClient(treasurerId);

        var preview = await treasurer
            .GetFromJsonAsync<CycleClosePreviewResponse>(
                PreviewUrl(data.StokvelId, data.CycleId));

        Assert.NotNull(preview);

        using var close = await CloseAsync(
            treasurer,
            data.StokvelId,
            data.CycleId,
            preview.Version,
            Guid.NewGuid().ToString("N"));

        Assert.Equal(
            HttpStatusCode.OK,
            close.StatusCode);

        using var member =
            _factory.CreateAuthenticatedClient(
                data.UserIds.First());

        using var recording = new HttpRequestMessage(
            HttpMethod.Post,
            $"/api/v1/stokvels/{data.StokvelId}/cycles/{data.CycleId}/contributions");

        recording.Headers.Add(
            "Idempotency-Key",
            Guid.NewGuid().ToString("N"));

        recording.Content = JsonContent.Create(new
        {
            UserId = data.UserIds.First(),
            AmountInCents = 50_000L
        });

        using var response =
            await member.SendAsync(recording);

        Assert.Equal(
            HttpStatusCode.Conflict,
            response.StatusCode);
    }

    [Fact]
    public async Task Fully_paid_cycle_closes_with_zero_arrears()
    {
        var (data, treasurerId) = await CreateScenarioAsync();

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider
                .GetRequiredService<RondiTrackDbContext>();

            foreach (
                var userId in data.UserIds.Append(treasurerId))
            {
                db.Contributions.Add(
                    new Contribution(
                        data.StokvelId,
                        userId,
                        data.CycleId,
                        500m));
            }

            await db.SaveChangesAsync();
        }

        using var treasurer =
            _factory.CreateAuthenticatedClient(treasurerId);

        var preview = await treasurer
            .GetFromJsonAsync<CycleClosePreviewResponse>(
                PreviewUrl(data.StokvelId, data.CycleId));

        Assert.NotNull(preview);
        Assert.Empty(preview.Arrears);
        Assert.Equal(
            0L,
            preview.TotalArrearsInCents);

        using var response = await CloseAsync(
            treasurer,
            data.StokvelId,
            data.CycleId,
            preview.Version,
            Guid.NewGuid().ToString("N"));

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var result = await response.Content
            .ReadFromJsonAsync<CloseCycleResponse>();

        Assert.NotNull(result);
        Assert.Equal(0, result.ArrearsCount);
        Assert.Equal(
            0L,
            result.TotalArrearsInCents);
    }

    
    [Fact]
    public async Task Concurrent_identical_close_requests_complete_once()
    {
        var (data, treasurerId) = await CreateScenarioAsync();

        using var treasurer =
            _factory.CreateAuthenticatedClient(treasurerId);

        var preview = await treasurer
            .GetFromJsonAsync<CycleClosePreviewResponse>(
                PreviewUrl(data.StokvelId, data.CycleId));

        Assert.NotNull(preview);

        var key = Guid.NewGuid().ToString("N");

        var firstRequest = CloseAsync(
            treasurer,
            data.StokvelId,
            data.CycleId,
            preview.Version,
            key);

        var secondRequest = CloseAsync(
            treasurer,
            data.StokvelId,
            data.CycleId,
            preview.Version,
            key);

        var responses = await Task.WhenAll(
            firstRequest,
            secondRequest);

        using var first = responses[0];
        using var second = responses[1];

        Assert.Equal(
            HttpStatusCode.OK,
            first.StatusCode);

        Assert.Equal(
            HttpStatusCode.OK,
            second.StatusCode);

        Assert.Equal(
            await first.Content.ReadAsStringAsync(),
            await second.Content.ReadAsStringAsync());

        using var scope = _factory.Services.CreateScope();

        var db = scope.ServiceProvider
            .GetRequiredService<RondiTrackDbContext>();

        Assert.Equal(
            1,
            await db.CycleCloseReceipts.CountAsync(
                item => item.CycleId == data.CycleId));

        Assert.Equal(
            3,
            await db.Arrears.CountAsync(
                item => item.ContributionCycleId == data.CycleId));

        Assert.Equal(
            1,
            await db.ContributionCycles.CountAsync(
                item =>
                    item.StokvelId == data.StokvelId &&
                    item.CycleNumber == 2));
    }


    private async Task<(TestScenario Data, Guid TreasurerId)>
        CreateScenarioAsync()
    {
        var data = await TestData.CreateAsync(
            _factory.Services,
            memberCount: 2,
            createContributions: false);

        using var scope = _factory.Services.CreateScope();

        var db = scope.ServiceProvider
            .GetRequiredService<RondiTrackDbContext>();

        var unique = Guid.NewGuid().ToString("N");

        var treasurer = new User(
            "Cycle Close Treasurer",
            $"close-{unique}@ronditrack.test",
            "0821234567");

        db.Users.Add(treasurer);

        db.StokvelMembers.Add(
            new StokvelMember(
                data.StokvelId,
                treasurer.Id,
                StokvelMemberRole.Treasurer));

        var cycle = await db.ContributionCycles
            .SingleAsync(item => item.Id == data.CycleId);

        cycle.SetEndDate(new DateOnly(2099, 1, 25));

        await db.SaveChangesAsync();

        return (data, treasurer.Id);
    }

    private static string PreviewUrl(
        Guid stokvelId,
        Guid cycleId)
    {
        return
            $"/api/v1/stokvels/{stokvelId}" +
            $"/cycles/{cycleId}/close-preview";
    }

    private static async Task<HttpResponseMessage> CloseAsync(
        HttpClient client,
        Guid stokvelId,
        Guid cycleId,
        string previewVersion,
        string key)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"/api/v1/stokvels/{stokvelId}/cycles/{cycleId}/close");

        request.Headers.Add(
            "Idempotency-Key",
            key);

        request.Content = JsonContent.Create(
            new CloseCycleRequest(previewVersion));

        return await client.SendAsync(request);
    }
}
