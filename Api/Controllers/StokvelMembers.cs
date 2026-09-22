using Microsoft.AspNetCore.Mvc;
using Api.Data;
using Api.Domain;

namespace Api.Controllers;

[ApiController]
[Route("api/stokvels/{stokvelId:guid}/members")]
public sealed class StokvelMembersController : ControllerBase
{
    private readonly ITrackStore _store;

    public StokvelMembersController(ITrackStore store)
    {
        _store = store;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<User>>>
        GetMembers(Guid stokvelId)
    {
        var stokvel =
            await _store.GetStokvelByIdAsync(stokvelId);

        if (stokvel is null)
        {
            return NotFound();
        }

        return Ok(stokvel.Members);
    }

    [HttpGet("{userId:guid}")]
    public async Task<ActionResult<User>>
        GetMember(
            Guid stokvelId,
            Guid userId)
    {
        var stokvel =
            await _store.GetStokvelByIdAsync(stokvelId);

        if (stokvel is null)
        {
            return NotFound();
        }

        var member =
            stokvel.Members.FirstOrDefault(
                user => user.Id == userId);

        if (member is null)
        {
            return NotFound();
        }

        return Ok(member);
    }

    [HttpPost("{userId:guid}")]
    public async Task<IActionResult>
        AddMember(Guid stokvelId, Guid userId)
    {
        var stokvel =
            await _store.GetStokvelByIdAsync(stokvelId);

        if (stokvel is null)
        {
            return NotFound(new
            {
                message = "Stokvel not found."
            });
        }

        var user =
            await _store.GetUserByIdAsync(userId);

        if (user is null)
        {
            return NotFound(new
            {
                message = "User not found."
            });
        }

        try
        {
            stokvel.AddMember(user);
        }
        catch (InvalidOperationException exception)
        {
            return Conflict(new
            {
                message = exception.Message
            });
        }

        await _store.UpdateStokvelAsync(stokvel);

        return CreatedAtAction(
            nameof(GetMember),
            new
            {
                stokvelId,
                userId
            },
            user);
    }

    [HttpDelete("{userId:guid}")]
    public async Task<IActionResult>
        RemoveMember(
            Guid stokvelId,
            Guid userId)
    {
        var stokvel =
            await _store.GetStokvelByIdAsync(stokvelId);

        if (stokvel is null)
        {
            return NotFound();
        }

        var removed =
            stokvel.RemoveMember(userId);

        if (!removed)
        {
            return NotFound(new
            {
                message =
                    "The user is not a member of this stokvel."
            });
        }

        await _store.UpdateStokvelAsync(stokvel);

        return NoContent();
    }
}