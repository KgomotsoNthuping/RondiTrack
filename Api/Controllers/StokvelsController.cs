using Microsoft.AspNetCore.Mvc;
using Api.Data;
using Api.Domain;

namespace Api.Controllers;

[ApiController]
[Route("api/stokvels")]
public sealed class StokvelsController : ControllerBase
{
    private readonly ITrackStore _store;

    public StokvelsController(ITrackStore store)
    {
        _store = store;
    }

    // api/stokvels
    //get all stokvels
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
        var stokvel = await _store.GetStokvelByIdAsync(id);

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
    public async Task<ActionResult<Stokvel>>
        Update(
            Guid id,
            [FromBody] Stokvel updatedStokvel)
    {
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

        await _store.UpdateStokvelAsync(existingStokvel);

        return Ok(existingStokvel);
    }
    // /api/stokvels
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult>
        Delete(Guid id)
    {
        var deleted = await _store.DeleteStokvelAsync(id);

        if (!deleted)
        {
            return NotFound();
        }

        return NoContent();
    }
}