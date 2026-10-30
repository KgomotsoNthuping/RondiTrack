using Microsoft.AspNetCore.Mvc;
using Api.DTO;
using Api.Data;
using Api.Mappings;
using Api.Services;
using Api.Extensions;
using Api.Validation;
using Domain.Repositories;
using Api.Auth;
using Api.Common;
using Microsoft.AspNetCore.RateLimiting;
using System.Globalization;
using Domain.Exceptions;

namespace Api.Controllers;

[ApiController]
[Route(ApiRoutes.V1 + "/stokvels/{stokvelId:guid}/contributions")]
public sealed class ContributionsController : ControllerBase
{
    private readonly ITrackStore _store;
    private readonly ITrackService _service;
    private readonly StokvelAccessAuthorizer _access;
    private readonly ContributionListService _listService;

    public ContributionsController(
        ITrackStore store,
        ITrackService service,
        StokvelAccessAuthorizer access,
        ContributionListService listService)
    {
        _store = store;
        _service = service;
        _access = access;
        _listService = listService;
    }

    
    [HttpGet]
    public async Task<
        ActionResult<PagedResponse<ContributionListItemResponse>>>
        GetAll(
            Guid stokvelId,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? pageToken = null,
            [FromQuery] string? member = null,
            [FromQuery(Name = "from")] string? fromValue = null,
            [FromQuery(Name = "to")] string? toValue = null)
    {
        if (!await _access.AuthorizeAsync(
            User,
            stokvelId,
            Policies.StokvelMember))
        {
            return Forbid();
        }

        Guid? memberId = null;

        if (!string.IsNullOrWhiteSpace(member))
        {
            if (
                !Guid.TryParse(member, out var parsedMember) ||
                parsedMember == Guid.Empty
            )
            {
                throw new ValidationException(
                    new Dictionary<string, string[]>
                    {
                        ["member"] = ["Select a valid member."]
                    });
            }

            memberId = parsedMember;
        }

        var fromUtc = ParseUtcInstant(
            fromValue,
            "from");

        var toUtc = ParseUtcInstant(
            toValue,
            "to");

        var request = new ContributionHistoryPageRequest(
            pageSize,
            memberId,
            fromUtc,
            toUtc,
            pageToken);

        var result = await _listService.GetAsync(
            stokvelId,
            request);

        return Ok(result);
    }

    private static DateTime? ParseUtcInstant(
        string? value,
        string field)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var formats = new[]
        {
            "yyyy-MM-dd'T'HH:mm:ss'Z'",
            "yyyy-MM-dd'T'HH:mm:ss.FFFFFFF'Z'"
        };

        if (!DateTime.TryParseExact(
            value,
            formats,
            CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal |
            DateTimeStyles.AdjustToUniversal,
            out var result))
        {
            throw new ValidationException(
                new Dictionary<string, string[]>
                {
                    [field] =
                    [
                        "Enter a valid UTC instant."
                    ]
                });
        }

        return DateTime.SpecifyKind(
            result,
            DateTimeKind.Utc);
    }

    // GET /api/stokvels/{stokvelId}/contributions/{contributionId}
    [HttpGet("{contributionId:guid}")]
    public async Task<ActionResult<ContributionResponse>>
        GetById(
            Guid stokvelId,
            Guid contributionId)
    {
        if (!await _access.AuthorizeAsync(
        User,
        stokvelId,
        Policies.StokvelMember))
        {
            return Forbid();
        }
        var contribution =
            await _store.GetContributionByIdAsync(
                stokvelId,
                contributionId);

        if (contribution is null)
        {
            return this.NotFoundProblem(
                "The contribution was not found.");
        }

        return Ok(contribution.ToResponse());
    }

    // POST /api/stokvels/{stokvelId}/contributions
    [HttpPost]
    [RequireIdempotencyKey]
    [EnableRateLimiting(RateLimitPolicies.Contribution)]
    public async Task<ActionResult<ContributionResponse>>
    RecordContribution(
        Guid stokvelId,
        [FromHeader(Name = "Idempotency-Key")]
        string idempotencyKey,
        RecordContributionRequest request)
    {
        if (!await _access.AuthorizeAsync(
                User,
                stokvelId,
                Policies.StokvelMember))
        {
            return Forbid();
        }

        var callerUserId =
            Guid.Parse(
                User.FindFirst("sub")!.Value);

        if (request.UserId != callerUserId)
        {
            if (!await _access.AuthorizeAsync(
                    User,
                    stokvelId,
                    Policies.StokvelAdmin))
            {
                return Forbid();
            }
        }
        
        var contribution = await _service.RecordContributionAsync(
            stokvelId,
            request.UserId,
            request.ContributionCycleId,
            request.Amount,
            idempotencyKey);

        var response = contribution.ToResponse();

        return CreatedAtAction(
        nameof(GetById),
        new
        {
            stokvelId,
            contributionId = response.Id
        },
        response);
    }
}