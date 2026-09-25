using Microsoft.AspNetCore.Mvc;
using Api.DTO;
using Api.Data;
using Api.Extensions;
using Api.Mappings;
using Api.Services;

namespace Api.Controllers;

[ApiController]
[Route("api/stokvels/{stokvelId:guid}/contributions")]
public sealed class ContributionsController
    : ControllerBase
{
    private readonly ITrackStore _store;
    private readonly ITrackService _service;

    public ContributionsController(
        ITrackStore store,
        ITrackService service)
    {
        _store = store;
        _service = service;
    }

    // GET /api/stokvels/{stokvelId}/contributions/{contributionId}
    [HttpGet("{contributionId:guid}")]
    public async Task<ActionResult<ContributionResponse>>
        GetById(
            Guid stokvelId,
            Guid contributionId)
    {
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
    public async Task<ActionResult<ContributionResponse>>
        RecordContribution(
            Guid stokvelId,
            [FromHeader(Name = "Idempotency-Key")]
            string? idempotencyKey,
            RecordContributionRequest request)
    {
        var result =
            await _service.RecordContributionAsync(
                stokvelId,
                request.UserId,
                request.CycleNumber,
                request.Amount,
                idempotencyKey);

        if (!result.IsSuccess)
        {
            return this.ToProblem(result);
        }

        var response =
            result.Value!.ToResponse();

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