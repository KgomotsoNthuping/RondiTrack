using Api.Auth;
using Api.Common;
using Api.DTO;
using Domain.Models;
using Domain.Repositories;
using Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Api.Controllers;

[ApiController]
[Route(ApiRoutes.V1 + "/stokvels")]
public sealed class StokvelsController : ControllerBase
{
    private readonly ITrackStore _store;
    private readonly StokvelAccessAuthorizer _access;
    private readonly RondiTrackDbContext _db;

    public StokvelsController(
        ITrackStore store,
        StokvelAccessAuthorizer access,
        RondiTrackDbContext db)
    {
        _store = store;
        _access = access;
        _db = db;
    }

    [Authorize(Roles = nameof(PlatformRole.Admin))]
    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<Stokvel>>>
        GetAll()
    {
        var stokvels = await _store.GetStokvelsAsync();

        return Ok(stokvels);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<StokvelDetailResponse>>
        GetById(Guid id)
    {
        if (!await _access.AuthorizeAsync(
            User,
            id,
            Policies.StokvelMember))
        {
            return Forbid();
        }

        if (!Guid.TryParse(
            User.FindFirst("sub")?.Value,
            out var userId))
        {
            return Unauthorized();
        }

        var stokvel = await _store.GetStokvelByIdAsync(
            id,
            asNoTracking: true);

        if (stokvel is null)
        {
            return NotFound();
        }

        var membership = await _store.GetStokvelMemberAsync(
            id,
            userId);

        if (membership is null)
        {
            return NotFound();
        }

        var memberCount = await _db.StokvelMembers
            .AsNoTracking()
            .CountAsync(member => member.StokvelId == id);

        var cycle = await _db.ContributionCycles
            .AsNoTracking()
            .Where(item =>
                item.StokvelId == id &&
                item.CycleNumber == stokvel.CurrentPeriod)
            .Select(item => new
            {
                item.Id,
                item.CycleNumber,
                item.Status,
                item.EndDate,
                item.ClosedAtUtc,
                item.TargetAmount
            })
            .FirstOrDefaultAsync();

        StokvelCycleSummaryResponse? cycleSummary = null;

        if (cycle is not null)
        {
            var totalCollected = await _db.Contributions
                .AsNoTracking()
                .Where(contribution =>
                    contribution.ContributionCycleId == cycle.Id &&
                    contribution.StokvelId == id &&
                    contribution.Status != ContributionStatus.Pending)
                .SumAsync(contribution =>
                    (decimal?)contribution.Amount) ?? 0m;

            var status = cycle.Status ==
                ContributionCycleStatus.PaidOut
                    ? "PaidOut"
                    : cycle.ClosedAtUtc.HasValue
                        ? "Closed"
                        : "Open";

            cycleSummary = new StokvelCycleSummaryResponse(
                cycle.Id,
                cycle.CycleNumber,
                status,
                cycle.EndDate,
                ToCents(cycle.TargetAmount),
                ToCents(totalCollected),
                ToCents(Math.Max(
                    0m,
                    cycle.TargetAmount - totalCollected)));
        }

        var response = new StokvelDetailResponse(
            stokvel.Id,
            stokvel.Name,
            ToCents(stokvel.MonthlyContribution),
            membership.Role.ToString(),
            memberCount,
            stokvel.CurrentPeriod,
            stokvel.TotalPeriods,
            stokvel.Rules,
            cycleSummary);

        return Ok(response);
    }

    [HttpPost]
    public async Task<ActionResult<Stokvel>>
        Create([FromBody] Stokvel stokvel)
    {
        await _store.AddStokvelAsync(stokvel);

        return CreatedAtAction(
            nameof(GetById),
            new { id = stokvel.Id },
            stokvel);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<Stokvel>> Update(
        Guid id,
        [FromBody] Stokvel updatedStokvel)
    {
        if (!await _access.AuthorizeAsync(
            User,
            id,
            Policies.StokvelAdmin))
        {
            return Forbid();
        }

        var existingStokvel =
            await _store.GetStokvelByIdAsync(id);

        if (existingStokvel is null)
        {
            return NotFound();
        }

        try
        {
            existingStokvel.UpdateDetails(
                updatedStokvel.Name,
                updatedStokvel.MonthlyContribution,
                updatedStokvel.CurrentPeriod,
                updatedStokvel.TotalPeriods,
                updatedStokvel.Rules);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new
            {
                message = exception.Message
            });
        }

        await _store.UpdateStokvelAsync(
            existingStokvel,
            updatedStokvel.Version);

        return Ok(existingStokvel);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        if (!await _access.AuthorizeAsync(
            User,
            id,
            Policies.StokvelAdmin))
        {
            return Forbid();
        }

        var deleted = await _store.DeleteStokvelAsync(id);

        if (!deleted)
        {
            return NotFound();
        }

        return NoContent();
    }

    private static long ToCents(decimal amount)
    {
        return checked((long)decimal.Round(
            amount * 100m,
            0,
            MidpointRounding.AwayFromZero));
    }
}
