using Microsoft.AspNetCore.Mvc;
using Domain.Models;
using Domain.Repositories;
using Api.Auth;
using Microsoft.AspNetCore.Authorization;

namespace Api.Controllers;

[ApiController]
[Route("api/stokvels")]
public sealed class StokvelsController : ControllerBase
{
    private readonly ITrackStore _store;

    private readonly StokvelAccessAuthorizer _access;

    public StokvelsController(ITrackStore store, StokvelAccessAuthorizer access)
    {
        _store = store;
        _access = access;
    }

    // api/stokvels
    //get all stokvels
    [Authorize(Roles = nameof(PlatformRole.Admin))]
    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<Stokvel>>>
        GetAll()
    {
        var stokvels = await _store.GetStokvelsAsync();

        return Ok(stokvels);
    }

    // api/stokvels/id
    // Get stokvels by id
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<Stokvel>>
        GetById(Guid id)
    {
        if (!await _access.AuthorizeAsync(User, id, Policies.StokvelMember))
        {
            return Forbid();
        }

        var stokvel = await _store.GetStokvelByIdAsync(id, asNoTracking: true);

        if (stokvel is null)
        {
            return NotFound();
        }

        return Ok(stokvel);
    }

    //POST /api/stokvels
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

    //PUT /api/stokvels
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<Stokvel>> Update(Guid id, [FromBody] Stokvel updatedStokvel)
    {
        if (!await _access.AuthorizeAsync(User, id, Policies.StokvelAdmin))
        {
            return Forbid();
        }
        var existingStokvel = await _store.GetStokvelByIdAsync(id);

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
            return BadRequest(new { message = exception.Message });
        }

        await _store.UpdateStokvelAsync(existingStokvel, updatedStokvel.Version);

        return Ok(existingStokvel);
    }
    // /api/stokvels
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        if (!await _access.AuthorizeAsync(User, id, Policies.StokvelAdmin))
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
}