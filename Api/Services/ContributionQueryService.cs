using System.Text;
using System.Text.Json;
using Api.Data;
using Api.DTO;
using Domain.Exceptions;
using Domain.Models;
using Microsoft.EntityFrameworkCore;
using Infrastructure.Data;

namespace Api.Services;

public sealed class ContributionQueryService : IContributionQueryService
{
    private const int DefaultPageSize = 20;
    private const int MaximumPageSize = 100;

    private readonly RondiTrackDbContext _dbContext;

    public ContributionQueryService(RondiTrackDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<
        PagedResponse<CycleContributionResponse>> GetCycleContributionsAsync(
            Guid stokvelId,
            Guid cycleId,
            CycleContributionPageRequest request)
    {
        var cycleExists = await _dbContext.ContributionCycles
                .AsNoTracking()
                .AnyAsync(cycle => cycle.Id == cycleId &&
                    cycle.StokvelId == stokvelId);

        if (!cycleExists)
        {
            throw new ResourceNotFoundException("The contribution cycle was not found.");
        }

        var pageSize = ResolvePageSize(request.PageSize);

        var sort = ResolveSort(request.Sort);

        var memberRole = ResolveMemberRole(request.MemberRole);

        var pageToken = DecodePageToken(request.PageToken);

        ValidatePageToken(
            pageToken,
            stokvelId,
            cycleId,
            sort,
            request.UserId,
            memberRole);

        IQueryable<Contribution> query = _dbContext.Contributions
                .AsNoTracking()
                .Where(contribution => contribution.StokvelId == stokvelId && 
                       contribution.ContributionCycleId == cycleId);

        if (request.UserId.HasValue)
        {
            query = query.Where(contribution => contribution.UserId == request.UserId.Value);
        }

        if (memberRole.HasValue)
        {
            query = query.Where(contribution => contribution.Member.Role == memberRole.Value);
        }

        query = ApplyPageToken(query, pageToken, sort);

        query = ApplySort(query, sort);

        var results =
            await query
                .Select(contribution =>
                    new CycleContributionResponse(
                        contribution.Id,
                        contribution.UserId,
                        contribution
                            .Member
                            .User
                            .FullName,
                        contribution.Member.Role,
                        contribution.Amount))
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
            hasMore && results.Count > 0
                ? EncodePageToken(
                    CreatePageToken(
                        results[^1],
                        stokvelId,
                        cycleId,
                        sort,
                        request.UserId,
                        memberRole))
                : string.Empty;

        return new PagedResponse<
            CycleContributionResponse>(
                results,
                nextPageToken);
    }

    private static int ResolvePageSize(
        int? requestedPageSize)
    {
        if (!requestedPageSize.HasValue ||
            requestedPageSize.Value == 0)
        {
            return DefaultPageSize;
        }

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
                ? "userId"
                : requestedSort.Trim();

        if (sort != "userId" &&
            sort != "amount")
        {
            throw new ValidationException(
                new Dictionary<
                    string,
                    string[]>
                {
                    ["sort"] =
                    [
                        "Sort must be 'userId' or 'amount'."
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
                true,
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

    private static IQueryable<Contribution>
        ApplySort(
            IQueryable<Contribution> query,
            string sort)
    {
        return sort switch
        {
            "amount" =>
                query
                    .OrderBy(contribution =>
                        contribution.Amount)
                    .ThenBy(contribution =>
                        contribution.Id),

            _ =>
                query
                    .OrderBy(contribution =>
                        contribution.UserId)
                    .ThenBy(contribution =>
                        contribution.Id)
        };
    }

    private static IQueryable<Contribution>
        ApplyPageToken(
            IQueryable<Contribution> query,
            ContributionPageToken? token,
            string sort)
    {
        if (token is null)
        {
            return query;
        }

        if (sort == "amount")
        {
            return query.Where(
                contribution =>
                    contribution.Amount >
                        token.LastAmount ||
                    (
                        contribution.Amount ==
                            token.LastAmount &&
                        contribution.Id >
                            token.LastId
                    ));
        }

        return query.Where(
            contribution =>
                contribution.UserId >
                    token.LastUserId ||
                (
                    contribution.UserId ==
                        token.LastUserId &&
                    contribution.Id >
                        token.LastId
                ));
    }

    private static ContributionPageToken
        CreatePageToken(
            CycleContributionResponse lastItem,
            Guid stokvelId,
            Guid cycleId,
            string sort,
            Guid? userId,
            StokvelMemberRole? memberRole)
    {
        return new ContributionPageToken(
            stokvelId,
            cycleId,
            sort,
            userId,
            memberRole,
            lastItem.UserId,
            lastItem.Amount,
            lastItem.Id);
    }

    private static void ValidatePageToken(
        ContributionPageToken? token,
        Guid stokvelId,
        Guid cycleId,
        string sort,
        Guid? userId,
        StokvelMemberRole? memberRole)
    {
        if (token is null)
        {
            return;
        }

        if (token.StokvelId != stokvelId ||
            token.CycleId != cycleId ||
            token.Sort != sort ||
            token.UserIdFilter != userId ||
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
        ContributionPageToken token)
    {
        var json =
            JsonSerializer.Serialize(token);

        var bytes =
            Encoding.UTF8.GetBytes(json);

        return Convert
            .ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    private static ContributionPageToken?
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
                .Deserialize<
                    ContributionPageToken>(
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

    private sealed record ContributionPageToken(
        Guid StokvelId,
        Guid CycleId,
        string Sort,
        Guid? UserIdFilter,
        StokvelMemberRole? MemberRoleFilter,
        Guid LastUserId,
        decimal LastAmount,
        Guid LastId);
}