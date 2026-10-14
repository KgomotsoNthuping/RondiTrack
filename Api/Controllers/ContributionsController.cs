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

namespace Api.Controllers;

[ApiController]
[Route(ApiRoutes.V1 + "api/stokvels/{stokvelId:guid}/contributions")]
public sealed class ContributionsController : ControllerBase
{
    private readonly ITrackStore _store;
    private readonly ITrackService _service;
    private readonly StokvelAccessAuthorizer _access;

    public ContributionsController(
        ITrackStore store,
        ITrackService service,
        StokvelAccessAuthorizer access)
    {
        _store = store;
        _service = service;
        _access = access;
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