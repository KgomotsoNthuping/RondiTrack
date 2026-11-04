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
public sealed class StokvelInviteContractTests
{
    private readonly PostgresApiFactory _factory;

    public StokvelInviteContractTests(
        PostgresApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Only_treasurer_can_create_invitation()
    {
        var data = await TestData.CreateAsync(
            _factory.Services,
            memberCount: 2);

        var treasurerId = await AddTreasurerAsync(
            data.StokvelId);

        var url =
            $"/api/v1/stokvels/{data.StokvelId}/invites";

        using var treasurer =
            _factory.CreateAuthenticatedClient(
                treasurerId);

        using var ordinaryMember =
            _factory.CreateAuthenticatedClient(
                data.UserIds.Last());

        var created = await treasurer.PostAsync(
            url,
            null);

        var denied = await ordinaryMember.PostAsync(
            url,
            null);

        Assert.Equal(
            HttpStatusCode.Created,
            created.StatusCode);

        Assert.Equal(
            HttpStatusCode.Forbidden,
            denied.StatusCode);

        var result = await created.Content
            .ReadFromJsonAsync<CreateStokvelInviteResponse>();

        Assert.NotNull(result);
        Assert.Equal(43, result.Code.Length);
        Assert.True(
            result.ExpiresAtUtc > DateTime.UtcNow);
    }

    [Fact]
    public async Task Preview_does_not_expose_member_information()
    {
        var data = await TestData.CreateAsync(
            _factory.Services);

        var other = await TestData.CreateAsync(
            _factory.Services);

        var treasurerId = await AddTreasurerAsync(
            data.StokvelId);

        using var treasurer =
            _factory.CreateAuthenticatedClient(
                treasurerId);

        using var invitee =
            _factory.CreateAuthenticatedClient(
                other.UserIds.First());

        var created = await treasurer.PostAsync(
            $"/api/v1/stokvels/{data.StokvelId}/invites",
            null);

        var invitation = await created.Content
            .ReadFromJsonAsync<CreateStokvelInviteResponse>();

        Assert.NotNull(invitation);

        var response = await invitee.GetAsync(
            $"/api/v1/invites/{invitation.Code}");

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var preview = await response.Content
            .ReadFromJsonAsync<StokvelInvitePreviewResponse>();

        Assert.NotNull(preview);
        Assert.Equal(
            data.StokvelId,
            preview.StokvelId);

        Assert.False(preview.AlreadyMember);
        Assert.Equal(6, preview.MemberCount);
        Assert.True(
            preview.ContributionAmountInCents > 0);

        var json = await response.Content
            .ReadAsStringAsync();

        Assert.DoesNotContain(
            "email",
            json,
            StringComparison.OrdinalIgnoreCase);

        Assert.DoesNotContain(
            "balance",
            json,
            StringComparison.OrdinalIgnoreCase);

        Assert.DoesNotContain(
            "payoutOrder",
            json,
            StringComparison.OrdinalIgnoreCase);

        Assert.DoesNotContain(
            "member1_",
            json,
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Unknown_and_expired_codes_have_distinct_statuses()
    {
        var data = await TestData.CreateAsync(
            _factory.Services);

        var treasurerId = await AddTreasurerAsync(
            data.StokvelId);

        using var treasurer =
            _factory.CreateAuthenticatedClient(
                treasurerId);

        var created = await treasurer.PostAsync(
            $"/api/v1/stokvels/{data.StokvelId}/invites",
            null);

        var invitation = await created.Content
            .ReadFromJsonAsync<CreateStokvelInviteResponse>();

        Assert.NotNull(invitation);

        using var invitee =
            _factory.CreateAuthenticatedClient(
                Guid.NewGuid());

        var unknown = await invitee.GetAsync(
            $"/api/v1/invites/{new string('a', 43)}");

        Assert.Equal(
            HttpStatusCode.NotFound,
            unknown.StatusCode);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider
                .GetRequiredService<RondiTrackDbContext>();

            var invite = await db.StokvelInvites
                .SingleAsync();

            db.Entry(invite)
                .Property(item => item.ExpiresAtUtc)
                .CurrentValue =
                    DateTime.UtcNow.AddMinutes(-1);

            await db.SaveChangesAsync();
        }

        var expired = await invitee.GetAsync(
            $"/api/v1/invites/{invitation.Code}");

        Assert.Equal(
            HttpStatusCode.Gone,
            expired.StatusCode);
    }

    [Fact]
    public async Task Joining_twice_creates_one_membership()
    {
        var data = await TestData.CreateAsync(
            _factory.Services);

        var other = await TestData.CreateAsync(
            _factory.Services);

        var treasurerId = await AddTreasurerAsync(
            data.StokvelId);

        var joinerId = other.UserIds.First();

        using var treasurer =
            _factory.CreateAuthenticatedClient(
                treasurerId);

        using var joiner =
            _factory.CreateAuthenticatedClient(
                joinerId);

        var created = await treasurer.PostAsync(
            $"/api/v1/stokvels/{data.StokvelId}/invites",
            null);

        var invitation = await created.Content
            .ReadFromJsonAsync<CreateStokvelInviteResponse>();

        Assert.NotNull(invitation);

        var url =
            $"/api/v1/invites/{invitation.Code}/join";

        var first = await joiner.PostAsync(url, null);
        var second = await joiner.PostAsync(url, null);

        Assert.Equal(
            HttpStatusCode.OK,
            first.StatusCode);

        Assert.Equal(
            HttpStatusCode.Conflict,
            second.StatusCode);

        using var scope = _factory.Services.CreateScope();

        var db = scope.ServiceProvider
            .GetRequiredService<RondiTrackDbContext>();

        var count = await db.StokvelMembers
            .CountAsync(member =>
                member.StokvelId == data.StokvelId &&
                member.UserId == joinerId);

        Assert.Equal(1, count);

        var preview = await joiner.GetFromJsonAsync<
            StokvelInvitePreviewResponse>(
                $"/api/v1/invites/{invitation.Code}");

        Assert.NotNull(preview);
        Assert.True(preview.AlreadyMember);
    }

    [Fact]
    public async Task Invites_require_authentication()
    {
        var data = await TestData.CreateAsync(
            _factory.Services);

        using var anonymous = _factory.CreateClient();

        var create = await anonymous.PostAsync(
            $"/api/v1/stokvels/{data.StokvelId}/invites",
            null);

        var preview = await anonymous.GetAsync(
            $"/api/v1/invites/{new string('a', 43)}");

        var join = await anonymous.PostAsync(
            $"/api/v1/invites/{new string('a', 43)}/join",
            null);

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            create.StatusCode);

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            preview.StatusCode);

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            join.StatusCode);
    }

    private async Task<Guid> AddTreasurerAsync(
        Guid stokvelId)
    {
        using var scope = _factory.Services.CreateScope();

        var db = scope.ServiceProvider
            .GetRequiredService<RondiTrackDbContext>();

        var unique = Guid.NewGuid().ToString("N");

        var user = new User(
            "Invitation Treasurer",
            $"treasurer_{unique}@ronditrack.test",
            "0821234567");

        db.Users.Add(user);

        db.StokvelMembers.Add(
            new StokvelMember(
                stokvelId,
                user.Id,
                StokvelMemberRole.Treasurer));

        await db.SaveChangesAsync();

        return user.Id;
    }
}
