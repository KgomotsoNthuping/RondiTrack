using Microsoft.AspNetCore.Mvc;
using Api.DTO;
using Api.Data;
using Api.Exceptions;
using Api.Mappings;
using Api.Services;

namespace Api.Controllers;

[ApiController]
[Route("api/stokvels/{stokvelId:guid}/cycles")]
public sealed class ContributionsCycleController : ControllerBase
{
    private readonly ITrackStore _store;

    private readonly IContributionQueryService _contributionQueryService;

    public ContributionsCycleController(
        ITrackStore store,
        IContributionQueryService contributionQueryService)
    {
        _store = store;
        _contributionQueryService = contributionQueryService;
    }

    // GET /api/stokvels/{stokvelId}/cycles
    [HttpGet]
    public async Task<
        ActionResult<
            IReadOnlyCollection<
                ContributionCycleResponse>>>
        GetAll(Guid stokvelId)
    {
        await EnsureStokvelExists(stokvelId);

        var cycles =
            await _store
                .GetContributionCyclesAsync(
                    stokvelId);

        return Ok(
            cycles
                .Select(cycle =>
                    cycle.ToResponse())
                .ToList());
    }

    // GET /api/stokvels/{stokvelId}/cycles/{cycleId}
    [HttpGet("{cycleId:guid}")]
    public async Task<
        ActionResult<ContributionCycleResponse>>
        GetById(
            Guid stokvelId,
            Guid cycleId)
    {
        var cycle =
            await _store
                .GetContributionCycleByIdAsync(
                    stokvelId,
                    cycleId,
                    asNoTracking: true);

        if (cycle is null)
        {
            throw new ResourceNotFoundException(
                "The contribution cycle was not found.");
        }

        return Ok(cycle.ToResponse());
    }

    // GET /api/stokvels/{stokvelId}/cycles/{cycleId}/contributions
    [HttpGet("{cycleId:guid}/contributions")]
    public async Task< ActionResult<IReadOnlyCollection<CycleContributionResponse>>> GetContributions(
            Guid stokvelId,
            Guid cycleId)
    {
            var contributions =await _contributionQueryService.GetCycleContributionsAsync(
                    stokvelId,
                    cycleId);

            return Ok(contributions);
    }

    // POST /api/stokvels/{stokvelId}/cycles
    [HttpPost]
    public async Task<
        ActionResult<ContributionCycleResponse>>
        Create(
            Guid stokvelId,
            CreateContributionCycleRequest request)
    {
        await EnsureStokvelExists(stokvelId);

        var cycle =
            request.ToDomain(stokvelId);

        await _store
            .AddContributionCycleAsync(cycle);

        return CreatedAtAction(
            nameof(GetById),
            new
            {
                stokvelId,
                cycleId = cycle.Id
            },
            cycle.ToResponse());
    }

    // PUT /api/stokvels/{stokvelId}/cycles/{cycleId}
    [HttpPut("{cycleId:guid}")]
    public async Task<
        ActionResult<ContributionCycleResponse>>
        Update(
            Guid stokvelId,
            Guid cycleId,
            UpdateContributionCycleRequest request)
    {
        var cycle =
            await _store
                .GetContributionCycleByIdAsync(
                    stokvelId,
                    cycleId);

        if (cycle is null)
        {
            throw new ResourceNotFoundException(
                "The contribution cycle was not found.");
        }

        request.ApplyTo(cycle);

        await _store
            .UpdateContributionCycleAsync(cycle);

        return Ok(cycle.ToResponse());
    }

    // DELETE /api/stokvels/{stokvelId}/cycles/{cycleId}
    [HttpDelete("{cycleId:guid}")]
    public async Task<IActionResult>
        Delete(
            Guid stokvelId,
            Guid cycleId)
    {
        var deleted =
            await _store
                .DeleteContributionCycleAsync(
                    stokvelId,
                    cycleId);

        if (!deleted)
        {
            throw new ResourceNotFoundException(
                "The contribution cycle was not found.");
        }

        return NoContent();
    }

    // Resource existence check, not a complex business decision.
    private async Task EnsureStokvelExists(
        Guid stokvelId)
    {
        var stokvel =
            await _store
                .GetStokvelByIdAsync(stokvelId);

        if (stokvel is null)
        {
            throw new ResourceNotFoundException(
                "The stokvel was not found.");
        }
    }
}