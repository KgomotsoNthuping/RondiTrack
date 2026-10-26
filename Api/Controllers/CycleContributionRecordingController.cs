using System.ComponentModel.DataAnnotations;
using Api.Auth;
using Api.Common;
using Api.DTO;
using Api.Services;
using Api.Validation;
using Domain.Models;
using Domain.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Api.Controllers;

[ApiController]
[Route(
    ApiRoutes.V1 +
    "/stokvels/{stokvelId:guid}/cycles/{cycleId:guid}/contributions")]
public sealed class CycleContributionRecordingController
    : ControllerBase
{
    private readonly ITrackStore _store;
    private readonly ITrackService _service;
    private readonly StokvelAccessAuthorizer _access;

    public CycleContributionRecordingController(
        ITrackStore store,
        ITrackService service,
        StokvelAccessAuthorizer access)
    {
        _store = store;
        _service = service;
        _access = access;
    }

    [HttpPost]
    [RequireIdempotencyKey]
    [EnableRateLimiting(RateLimitPolicies.Contribution)]
    public async Task<ActionResult<RecordedCycleContributionResponse>>
        Record(
            Guid stokvelId,
            Guid cycleId,
            [FromHeader(Name = "Idempotency-Key")]
            [Required]
            string idempotencyKey,
            [FromBody]
            RecordCycleContributionRequest request)
    {
        if (!await _access.AuthorizeAsync(
            User,
            stokvelId,
            Policies.StokvelMember))
        {
            return Forbid();
        }

        if (!Guid.TryParse(
            User.FindFirst("sub")?.Value,
            out var callerUserId))
        {
            return Unauthorized();
        }

        var callerMembership =
            await _store.GetStokvelMemberAsync(
                stokvelId,
                callerUserId);

        if (callerMembership is null)
        {
            return Forbid();
        }

        if (request.UserId != callerUserId &&
            callerMembership.Role != StokvelMemberRole.Treasurer &&
            callerMembership.Role != StokvelMemberRole.Admin)
        {
            return Forbid();
        }

        var amount = request.AmountInCents / 100m;

        var contribution =
            await _service.RecordContributionAsync(
                stokvelId,
                request.UserId,
                cycleId,
                amount,
                idempotencyKey);

        var response =
            new RecordedCycleContributionResponse(
                contribution.Id,
                contribution.StokvelId,
                contribution.UserId,
                contribution.ContributionCycleId,
                checked((long)(
                    contribution.Amount * 100m)),
                contribution.Status.ToString(),
                contribution.PaidAtUtc);

        return Created(
            $"/api/v1/stokvels/{stokvelId}/contributions/{contribution.Id}",
            response);
    }
}
