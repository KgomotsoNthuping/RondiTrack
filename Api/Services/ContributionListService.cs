using System.Text;
using System.Text.Json;
using Api.DTO;
using Domain.Exceptions;
using Domain.Models;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Api.Services;

public sealed class ContributionListService
{
    private readonly RondiTrackDbContext _dbContext;

    public ContributionListService(
        RondiTrackDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PagedResponse<ContributionListItemResponse>>
        GetAsync(
            Guid stokvelId,
            ContributionHistoryPageRequest request)
    {
        if (request.PageSize is < 1 or > 100)
        {
            Invalid(
                "pageSize",
                "Page size must be between 1 and 100.");
        }

        if (request.FromUtc.HasValue &&
            request.ToUtc.HasValue &&
            request.FromUtc.Value >= request.ToUtc.Value)
        {
            Invalid(
                "to",
                "The end must be after the start.");
        }

        if (request.Member.HasValue)
        {
            var belongsToStokvel =
                await _dbContext.StokvelMembers
                    .AsNoTracking()
                    .AnyAsync(member =>
                        member.StokvelId == stokvelId &&
                        member.UserId == request.Member.Value);

            if (!belongsToStokvel)
            {
                Invalid(
                    "member",
                    "The member filter is invalid for this stokvel.");
            }
        }

        var cursor = DecodeCursor(request.PageToken);

        if (cursor is not null)
        {
            if (
                cursor.Version != 1 ||
                cursor.StokvelId != stokvelId ||
                cursor.Member != request.Member ||
                cursor.FromUtc != request.FromUtc ||
                cursor.ToUtc != request.ToUtc ||
                cursor.PageSize != request.PageSize ||
                cursor.LastId == Guid.Empty ||
                cursor.LastPaidAtUtc.HasValue &&
                cursor.LastPaidAtUtc.Value.Kind !=
                    DateTimeKind.Utc
            )
            {
                Invalid(
                    "pageToken",
                    "The page token does not match the current filters.");
            }
        }

        IQueryable<Contribution> query =
            _dbContext.Contributions
                .AsNoTracking()
                .Where(item =>
                    item.StokvelId == stokvelId);

        if (request.Member.HasValue)
        {
            var memberId = request.Member.Value;

            query = query.Where(item =>
                item.UserId == memberId);
        }

        if (request.FromUtc.HasValue)
        {
            var fromUtc = request.FromUtc.Value;

            query = query.Where(item =>
                item.PaidAtUtc >= fromUtc);
        }

        if (request.ToUtc.HasValue)
        {
            var toUtc = request.ToUtc.Value;

            query = query.Where(item =>
                item.PaidAtUtc < toUtc);
        }

        if (cursor is not null)
        {
            var lastId = cursor.LastId;

            if (cursor.LastPaidAtUtc.HasValue)
            {
                var lastPaidAtUtc =
                    cursor.LastPaidAtUtc.Value;

                query = query.Where(item =>
                    item.PaidAtUtc == null ||
                    item.PaidAtUtc < lastPaidAtUtc ||
                    (
                        item.PaidAtUtc == lastPaidAtUtc &&
                        item.Id < lastId
                    ));
            }
            else
            {
                query = query.Where(item =>
                    item.PaidAtUtc == null &&
                    item.Id < lastId);
            }
        }

        var rows = await query
            .OrderBy(item => item.PaidAtUtc == null)
            .ThenByDescending(item => item.PaidAtUtc)
            .ThenByDescending(item => item.Id)
            .Select(item => new
            {
                item.Id,
                item.UserId,
                item.ContributionCycleId,
                item.Amount,
                item.Status,
                item.PaidAtUtc,
                item.ContributionCycle.CycleNumber,
                MemberName = item.Member.User.FullName
            })
            .Take(request.PageSize + 1)
            .ToListAsync();

        var hasMore = rows.Count > request.PageSize;

        if (hasMore)
        {
            rows.RemoveAt(rows.Count - 1);
        }

        var items = rows
            .Select(item =>
                new ContributionListItemResponse(
                    item.Id,
                    item.ContributionCycleId,
                    checked((long)decimal.Round(
                        item.Amount * 100m,
                        0,
                        MidpointRounding.AwayFromZero)),
                    item.Status.ToString(),
                    item.PaidAtUtc,
                    item.CycleNumber,
                    item.UserId,
                    item.MemberName))
            .ToList();

        var nextPageToken = string.Empty;

        if (hasMore && rows.Count > 0)
        {
            var last = rows[^1];

            nextPageToken = EncodeCursor(
                new HistoryCursor(
                    1,
                    stokvelId,
                    request.Member,
                    request.FromUtc,
                    request.ToUtc,
                    request.PageSize,
                    last.PaidAtUtc,
                    last.Id));
        }

        return new PagedResponse<
            ContributionListItemResponse>(
                items,
                nextPageToken);
    }

    private static string EncodeCursor(
        HistoryCursor cursor)
    {
        var json = JsonSerializer.Serialize(cursor);

        return Convert
            .ToBase64String(
                Encoding.UTF8.GetBytes(json))
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    private static HistoryCursor? DecodeCursor(
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
            Invalid(
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

            var cursor =
                JsonSerializer.Deserialize<HistoryCursor>(
                    bytes);

            if (cursor is null)
            {
                Invalid(
                    "pageToken",
                    "The page token is invalid.");
            }

            return cursor;
        }
        catch (FormatException)
        {
            Invalid(
                "pageToken",
                "The page token is invalid.");

            throw;
        }
        catch (JsonException)
        {
            Invalid(
                "pageToken",
                "The page token is invalid.");

            throw;
        }
    }

    private static void Invalid(
        string field,
        string message)
    {
        throw new ValidationException(
            new Dictionary<string, string[]>
            {
                [field] = [message]
            });
    }

    private sealed record HistoryCursor(
        int Version,
        Guid StokvelId,
        Guid? Member,
        DateTime? FromUtc,
        DateTime? ToUtc,
        int PageSize,
        DateTime? LastPaidAtUtc,
        Guid LastId);
}
