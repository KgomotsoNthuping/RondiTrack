using System.Text;
using System.Text.Json;
using Api.DTO;
using Domain.Exceptions;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Api.Services;

public sealed class MemberActivityService
{
    private readonly RondiTrackDbContext _db;

    public MemberActivityService(
        RondiTrackDbContext db)
    {
        _db = db;
    }

    public async Task<PagedResponse<MemberActivityItemResponse>>
        GetAsync(
            Guid userId,
            int pageSize,
            string? pageToken)
    {
        if (pageSize is < 1 or > 100)
        {
            throw Invalid(
                "pageSize",
                "Page size must be between 1 and 100.");
        }

        var cursor = DecodeCursor(pageToken);

        if (cursor is not null &&
            (
                cursor.Version != 1 ||
                cursor.UserId != userId ||
                cursor.PageSize != pageSize ||
                cursor.Kind is < 1 or > 3 ||
                cursor.StokvelId == Guid.Empty ||
                cursor.Id == Guid.Empty ||
                cursor.OccurredAtUtc.Kind != DateTimeKind.Utc
            ))
        {
            throw Invalid(
                "pageToken",
                "The page token does not match this activity feed.");
        }

        var permittedStokvelIds = _db.StokvelMembers
            .AsNoTracking()
            .Where(member => member.UserId == userId)
            .Select(member => member.StokvelId);

        var contributions = _db.Contributions
            .AsNoTracking()
            .Where(item =>
                item.PaidAtUtc.HasValue &&
                permittedStokvelIds.Contains(item.StokvelId));

        var payouts = _db.Payouts
            .AsNoTracking()
            .Where(item =>
                permittedStokvelIds.Contains(item.StokvelId));

        var memberships = _db.StokvelMembers
            .AsNoTracking()
            .Where(item =>
                permittedStokvelIds.Contains(item.StokvelId));

        if (cursor is not null)
        {
            var lastTime = cursor.OccurredAtUtc;
            var lastKind = cursor.Kind;
            var lastStokvelId = cursor.StokvelId;
            var lastId = cursor.Id;

            contributions = contributions.Where(item =>
                item.PaidAtUtc!.Value < lastTime ||
                (
                    item.PaidAtUtc.Value == lastTime &&
                    (
                        1 < lastKind ||
                        (
                            lastKind == 1 &&
                            EF.Functions.LessThan(
                                ValueTuple.Create(
                                    item.StokvelId,
                                    item.Id),
                                ValueTuple.Create(
                                    lastStokvelId,
                                    lastId))
                        )
                    )
                ));

            payouts = payouts.Where(item =>
                item.PaidAtUtc < lastTime ||
                (
                    item.PaidAtUtc == lastTime &&
                    (
                        2 < lastKind ||
                        (
                            lastKind == 2 &&
                            EF.Functions.LessThan(
                                ValueTuple.Create(
                                    item.StokvelId,
                                    item.Id),
                                ValueTuple.Create(
                                    lastStokvelId,
                                    lastId))
                        )
                    )
                ));

            memberships = memberships.Where(item =>
                item.JoinedAtUtc < lastTime ||
                (
                    item.JoinedAtUtc == lastTime &&
                    (
                        3 < lastKind ||
                        (
                            lastKind == 3 &&
                            EF.Functions.LessThan(
                                ValueTuple.Create(
                                    item.StokvelId,
                                    item.UserId),
                                ValueTuple.Create(
                                    lastStokvelId,
                                    lastId))
                        )
                    )
                ));
        }

        var limit = pageSize + 1;

        var contributionRows = await contributions
            .OrderByDescending(item => item.PaidAtUtc)
            .ThenByDescending(item => item.StokvelId)
            .ThenByDescending(item => item.Id)
            .Take(limit)
            .Select(item => new ActivityRow(
                1,
                item.Id,
                item.StokvelId,
                item.Member.Stokvel.Name,
                item.UserId,
                item.Member.User.FullName,
                item.PaidAtUtc!.Value,
                item.Amount))
            .ToListAsync();

        var payoutRows = await payouts
            .OrderByDescending(item => item.PaidAtUtc)
            .ThenByDescending(item => item.StokvelId)
            .ThenByDescending(item => item.Id)
            .Take(limit)
            .Select(item => new ActivityRow(
                2,
                item.Id,
                item.StokvelId,
                item.RecipientMember.Stokvel.Name,
                item.RecipientUserId,
                item.RecipientMember.User.FullName,
                item.PaidAtUtc,
                item.Amount))
            .ToListAsync();

        var membershipRows = await memberships
            .OrderByDescending(item => item.JoinedAtUtc)
            .ThenByDescending(item => item.StokvelId)
            .ThenByDescending(item => item.UserId)
            .Take(limit)
            .Select(item => new ActivityRow(
                3,
                item.UserId,
                item.StokvelId,
                item.Stokvel.Name,
                item.UserId,
                item.User.FullName,
                item.JoinedAtUtc,
                null))
            .ToListAsync();

        var ordered = contributionRows
            .Concat(payoutRows)
            .Concat(membershipRows)
            .OrderByDescending(item => item.OccurredAtUtc)
            .ThenByDescending(item => item.Kind)
            .ThenByDescending(
                item => item.StokvelId.ToString("N"),
                StringComparer.Ordinal)
            .ThenByDescending(
                item => item.Id.ToString("N"),
                StringComparer.Ordinal)
            .Take(limit)
            .ToList();

        var hasMore = ordered.Count > pageSize;

        if (hasMore)
        {
            ordered.RemoveAt(ordered.Count - 1);
        }

        var items = ordered
            .Select(item => new MemberActivityItemResponse(
                item.Id,
                item.Kind switch
                {
                    1 => "Contribution",
                    2 => "Payout",
                    _ => "MemberJoined"
                },
                item.StokvelId,
                item.StokvelName,
                item.MemberId,
                item.MemberName,
                item.OccurredAtUtc,
                ToCents(item.Amount)))
            .ToList();

        var nextPageToken = string.Empty;

        if (hasMore && ordered.Count > 0)
        {
            var last = ordered[^1];

            nextPageToken = EncodeCursor(
                new ActivityCursor(
                    1,
                    userId,
                    pageSize,
                    last.OccurredAtUtc,
                    last.Kind,
                    last.StokvelId,
                    last.Id));
        }

        return new PagedResponse<MemberActivityItemResponse>(
            items,
            nextPageToken);
    }

    private static long? ToCents(
        decimal? amount)
    {
        if (!amount.HasValue)
        {
            return null;
        }

        return checked(
            (long)decimal.Round(
                amount.Value * 100m,
                0,
                MidpointRounding.AwayFromZero));
    }

    private static string EncodeCursor(
        ActivityCursor cursor)
    {
        var bytes = JsonSerializer.SerializeToUtf8Bytes(
            cursor);

        return Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    private static ActivityCursor? DecodeCursor(
        string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return null;
        }

        if (
            value.Length > 4096 ||
            value.Any(character =>
                !char.IsAsciiLetterOrDigit(character) &&
                character != '-' &&
                character != '_')
        )
        {
            throw Invalid(
                "pageToken",
                "The page token is invalid.");
        }

        try
        {
            var encoded = value
                .Replace('-', '+')
                .Replace('_', '/');

            encoded += new string(
                '=',
                (4 - encoded.Length % 4) % 4);

            var bytes = Convert.FromBase64String(
                encoded);

            return JsonSerializer.Deserialize<ActivityCursor>(
                bytes)
                ?? throw Invalid(
                    "pageToken",
                    "The page token is invalid.");
        }
        catch (FormatException)
        {
            throw Invalid(
                "pageToken",
                "The page token is invalid.");
        }
        catch (JsonException)
        {
            throw Invalid(
                "pageToken",
                "The page token is invalid.");
        }
    }

    private static ValidationException Invalid(
        string field,
        string message)
    {
        return new ValidationException(
            new Dictionary<string, string[]>
            {
                [field] = [message]
            });
    }

    private sealed record ActivityRow(
        int Kind,
        Guid Id,
        Guid StokvelId,
        string StokvelName,
        Guid MemberId,
        string MemberName,
        DateTime OccurredAtUtc,
        decimal? Amount);

    private sealed record ActivityCursor(
        int Version,
        Guid UserId,
        int PageSize,
        DateTime OccurredAtUtc,
        int Kind,
        Guid StokvelId,
        Guid Id);
}
