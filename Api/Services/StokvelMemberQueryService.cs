using System.Text;
using System.Text.Json;
using Api.DTO;
using Domain.Exceptions;
using Domain.Models;
using Microsoft.EntityFrameworkCore;
using Infrastructure.Data;

namespace Api.Services;

public sealed class StokvelMemberQueryService
    : IStokvelMemberQueryService
{
    private const int DefaultPageSize = 20;
    private const int MaximumPageSize = 100;

    private readonly RondiTrackDbContext _dbContext;

    public StokvelMemberQueryService(
        RondiTrackDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PagedResponse<UserResponse>>
        GetMembersAsync(
            Guid stokvelId,
            StokvelMemberPageRequest request)
    {
        // Make sure the parent stokvel exists.
        var stokvelExists =
            await _dbContext.Stokvels
                .AsNoTracking()
                .AnyAsync(stokvel =>
                    stokvel.Id == stokvelId);

        if (!stokvelExists)
        {
            throw new ResourceNotFoundException(
                "The stokvel was not found.");
        }

        var pageSize =
            ResolvePageSize(
                request.PageSize);

        var sort =
            ResolveSort(
                request.Sort);

        var memberRole =
            ResolveMemberRole(
                request.MemberRole);

        var pageToken =
            DecodePageToken(
                request.PageToken);

        // A page token may only be reused
        // with the query that created it.
        ValidatePageToken(
            pageToken,
            stokvelId,
            sort,
            memberRole);

        IQueryable<StokvelMember> query =
            _dbContext.StokvelMembers
                .AsNoTracking()
                .Where(member =>
                    member.StokvelId ==
                        stokvelId);

        // Optional role filter.
        if (memberRole.HasValue)
        {
            query =
                query.Where(member =>
                    member.Role ==
                        memberRole.Value);
        }

        // Continue after the last item
        // represented by the token.
        query =
            ApplyPageToken(
                query,
                pageToken,
                sort);

        // Apply deterministic ordering.
        query =
            ApplySort(
                query,
                sort);

        /*
         * Request one extra row.
         *
         * If pageSize is 20, PostgreSQL receives
         * a LIMIT for 21 rows.
         *
         * The extra row tells us whether another
         * page exists.
         */
        var results =
            await query
                .Select(member =>
                    new MemberQueryResult(
                        member.UserId,
                        member.User.FullName,
                        member.User.Email,
                        member.User.PhoneNumber,
                        member.JoinedAtUtc,
                        member.User.Version))
                .Take(pageSize + 1)
                .ToListAsync();

        var hasMore =
            results.Count > pageSize;

        if (hasMore)
        {
            results.RemoveAt(
                results.Count - 1);
        }

        var nextPageToken =
            hasMore &&
            results.Count > 0
                ? EncodePageToken(
                    CreatePageToken(
                        results[^1],
                        stokvelId,
                        sort,
                        memberRole))
                : string.Empty;

        var response =
            results
                .Select(member =>
                    new UserResponse(
                        member.UserId,
                        member.FullName,
                        member.Email,
                        member.PhoneNumber,
                        member.Version))
                .ToList();

        return new PagedResponse<UserResponse>(
            response,
            nextPageToken);
    }

    private static int ResolvePageSize(
        int? requestedPageSize)
    {
        // Omitted or zero means use the default.
        if (!requestedPageSize.HasValue ||
            requestedPageSize.Value == 0)
        {
            return DefaultPageSize;
        }

        // Negative page sizes are invalid.
        if (requestedPageSize.Value < 0)
        {
            throw new ValidationException(
                new Dictionary<
                    string,
                    string[]>
                {
                    ["pageSize"] =
                    [
                        "Page size cannot be negative."
                    ]
                });
        }

        // Requests above the maximum are
        // reduced rather than rejected.
        return Math.Min(
            requestedPageSize.Value,
            MaximumPageSize);
    }

    private static string ResolveSort(
        string? requestedSort)
    {
        var sort =
            string.IsNullOrWhiteSpace(
                requestedSort)
                ? "fullName"
                : requestedSort.Trim();

        if (sort != "fullName" &&
            sort != "joinedAt")
        {
            throw new ValidationException(
                new Dictionary<
                    string,
                    string[]>
                {
                    ["sort"] =
                    [
                        "Sort must be 'fullName' or 'joinedAt'."
                    ]
                });
        }

        return sort;
    }

    private static StokvelMemberRole?
        ResolveMemberRole(
            string? requestedRole)
    {
        if (string.IsNullOrWhiteSpace(
                requestedRole))
        {
            return null;
        }

        if (Enum.TryParse<
                StokvelMemberRole>(
                requestedRole,
                ignoreCase: true,
                out var role))
        {
            return role;
        }

        throw new ValidationException(
            new Dictionary<
                string,
                string[]>
            {
                ["memberRole"] =
                [
                    "The member role is invalid."
                ]
            });
    }

    private static IQueryable<StokvelMember>
        ApplySort(
            IQueryable<StokvelMember> query,
            string sort)
    {
        return sort switch
        {
            "joinedAt" =>
                query
                    .OrderBy(member =>
                        member.JoinedAtUtc)
                    .ThenBy(member =>
                        member.UserId),

            _ =>
                query
                    .OrderBy(member =>
                        member.User.FullName)
                    .ThenBy(member =>
                        member.UserId)
        };
    }

    private static IQueryable<StokvelMember>
        ApplyPageToken(
            IQueryable<StokvelMember> query,
            MemberPageToken? token,
            string sort)
    {
        if (token is null)
        {
            return query;
        }

        /*
         * joinedAt keyset:
         *
         * JoinedAtUtc > previous JoinedAtUtc
         *
         * OR
         *
         * JoinedAtUtc is equal and
         * UserId is greater.
         */
        if (sort == "joinedAt")
        {
            return query.Where(member =>
                member.JoinedAtUtc >
                    token.LastJoinedAtUtc ||
                (
                    member.JoinedAtUtc ==
                        token.LastJoinedAtUtc &&
                    member.UserId >
                        token.LastUserId
                ));
        }

        /*
         * fullName keyset.
         *
         * UserId is the unique tiebreaker when
         * two members have the same full name.
         */
        return query.Where(member =>
            string.Compare(
                member.User.FullName,
                token.LastFullName) > 0 ||
            (
                member.User.FullName ==
                    token.LastFullName &&
                member.UserId >
                    token.LastUserId
            ));
    }

    private static MemberPageToken
        CreatePageToken(
            MemberQueryResult lastItem,
            Guid stokvelId,
            string sort,
            StokvelMemberRole? memberRole)
    {
        return new MemberPageToken(
            stokvelId,
            sort,
            memberRole,
            lastItem.FullName,
            lastItem.JoinedAtUtc,
            lastItem.UserId);
    }

    private static void ValidatePageToken(
        MemberPageToken? token,
        Guid stokvelId,
        string sort,
        StokvelMemberRole? memberRole)
    {
        if (token is null)
        {
            return;
        }

        if (token.StokvelId != stokvelId ||
            token.Sort != sort ||
            token.MemberRoleFilter !=
                memberRole)
        {
            throw new ValidationException(
                new Dictionary<
                    string,
                    string[]>
                {
                    ["pageToken"] =
                    [
                        "The page token does not match the current query."
                    ]
                });
        }
    }

    private static string EncodePageToken(
        MemberPageToken token)
    {
        var json =
            JsonSerializer.Serialize(token);

        var bytes =
            Encoding.UTF8.GetBytes(json);

        // URL-safe Base64.
        return Convert
            .ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    private static MemberPageToken?
        DecodePageToken(
            string? pageToken)
    {
        if (string.IsNullOrWhiteSpace(
                pageToken))
        {
            return null;
        }

        try
        {
            var value =
                pageToken
                    .Replace('-', '+')
                    .Replace('_', '/');

            // Restore removed Base64 padding.
            value +=
                new string(
                    '=',
                    (4 - value.Length % 4) % 4);

            var bytes =
                Convert.FromBase64String(
                    value);

            var json =
                Encoding.UTF8.GetString(
                    bytes);

            return JsonSerializer
                .Deserialize<MemberPageToken>(
                    json)
                ?? throw new JsonException();
        }
        catch
        {
            throw new ValidationException(
                new Dictionary<
                    string,
                    string[]>
                {
                    ["pageToken"] =
                    [
                        "The page token is invalid."
                    ]
                });
        }
    }

    /*
     * Internal projection used by the query.
     *
     * JoinedAtUtc is included here because it is
     * needed to create the next-page token when
     * sorting by joinedAt.
     *
     * It does not need to be exposed in UserResponse.
     */
    private sealed record MemberQueryResult(
        Guid UserId,
        string FullName,
        string Email,
        string PhoneNumber,
        DateTime JoinedAtUtc,
        uint Version);

    /*
     * The token stores enough information to
     * continue the keyset query and to verify
     * that it is being reused with the same
     * filter and sort.
     */
    private sealed record MemberPageToken(
        Guid StokvelId,
        string Sort,
        StokvelMemberRole? MemberRoleFilter,
        string LastFullName,
        DateTime LastJoinedAtUtc,
        Guid LastUserId);
}