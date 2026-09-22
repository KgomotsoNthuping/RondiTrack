using Microsoft.AspNetCore.Mvc;
using Api.Data;
using Api.Domain;

namespace Api.Controllers;

[ApiController]
[Route("api/users")]
public sealed class UsersController : ControllerBase
{
    private readonly ITrackStore _store;

    public UsersController(ITrackStore store)
    {
        _store = store;
    }

    // Get /api/users
    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<User>>>
        GetAll()
    {
        var users = await _store.GetUsersAsync();

        return Ok(users);
    }

    // Get users by id
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<User>>
        GetById(Guid id)
    {
        var user = await _store.GetUserByIdAsync(id);

        if (user is null)
        {
            return NotFound();
        }

        return Ok(user);
    }

    // Post /api/users
    // Creates the user resource
    [HttpPost]
    public async Task<ActionResult<User>>
        Create([FromBody] User user)
    {
        await _store.AddUserAsync(user);

        return CreatedAtAction(
            nameof(GetById),
            new { id = user.Id },
            user);
    }

    //Update user resources
    // /api/users/id
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<User>>
        Update(
            Guid id,
            [FromBody] User updatedUser)
    {
        var existingUser = await _store.GetUserByIdAsync(id);

        if (existingUser is null)
        {
            return NotFound();
        }

        try
        {
            existingUser.UpdateProfile(
                updatedUser.FullName,
                updatedUser.Email,
                updatedUser.PhoneNumber);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(
                new { message = exception.Message });
        }

        await _store.UpdateUserAsync(existingUser);

        return Ok(existingUser);
    }

    //delete users
    //api/users/id
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult>
        Delete(Guid id)
    {
        var user = await _store.GetUserByIdAsync(id);

        if (user is null)
        {
            return NotFound();
        }

        //Stops deletion if user still belongs to a stokvel
        var belongsToStokvel = await _store.IsUserMemberOfAnyStokvelAsync(id);

        if (belongsToStokvel)
        {
            return Conflict(new
            {
                message = "The user cannot be deleted while they are still a member of a stokvel."
            });
        }

        await _store.DeleteUserAsync(id);

        return NoContent();
    }
}