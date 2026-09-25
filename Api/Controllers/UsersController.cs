using Microsoft.AspNetCore.Mvc;
using Api.DTO;
using Api.Data;
using Api.Extensions;
using Api.Mappings;

namespace Api.Controllers;

[ApiController]
[Route("api/stokvels")]
public sealed class StokvelsController : ControllerBase
{
    private readonly ITrackStore _store;

    public StokvelsController(
        ITrackStore store)
    {
        _store = store;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<StokvelResponse>>>
        GetAll()
    {
        var stokvels =
            await _store.GetStokvelsAsync();

        var response =
            stokvels
                .Select(stokvel => stokvel.ToResponse())
                .ToList();

        return Ok(response);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<StokvelResponse>>
        GetById(Guid id)
    {
        var stokvel =
            await _store.GetStokvelByIdAsync(id);

        if (stokvel is null)
        {
            return this.NotFoundProblem(
                "The stokvel was not found.");
        }

        return Ok(stokvel.ToResponse());
    }

    [HttpPost]
    public async Task<ActionResult<StokvelResponse>>
        Create(CreateStokvelRequest request)
    {
        try
        {
            var stokvel =
                request.ToDomain();

            await _store.AddStokvelAsync(stokvel);

            var response =
                stokvel.ToResponse();

            return CreatedAtAction(
                nameof(GetById),
                new { id = stokvel.Id },
                response);
        }
        catch (ArgumentException exception)
        {
            return this.UnprocessableProblem(
                exception.Message);
        }
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<StokvelResponse>>
        Update(
            Guid id,
            UpdateStokvelRequest request)
    {
        var stokvel =
            await _store.GetStokvelByIdAsync(id);

        if (stokvel is null)
        {
            return this.NotFoundProblem(
                "The stokvel was not found.");
        }

        try
        {
            request.ApplyTo(stokvel);

            await _store.UpdateStokvelAsync(stokvel);

            return Ok(stokvel.ToResponse());
        }
        catch (ArgumentException exception)
        {
            return this.UnprocessableProblem(
                exception.Message);
        }
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult>
        Delete(Guid id)
    {
        var deleted =
            await _store.DeleteStokvelAsync(id);

        if (!deleted)
        {
            return this.NotFoundProblem(
                "The stokvel was not found.");
        }

        return NoContent();
    }
}