using Microsoft.AspNetCore.Mvc;
using Api.DTO;
using Api.Data;
using Api.Exceptions;
using Api.Mappings;
using Api.Services;

namespace Api.Controllers;

[ApiController]
[Route("api/users")]
public sealed class UsersController : ControllerBase
{
    private readonly ITrackStore _store;
    private readonly ITrackService _service;

    // The store handles simple data access.
    // The service handles operations that require business decisions.
    public UsersController(
        ITrackStore store,
        ITrackService service)
    {
        _store = store;
        _service = service;
    }

    // GET /api/users
    // Returns all Users as response DTOs rather than exposing domain entities.
    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<UserResponse>>>
        GetAll()
    {
        var users =
            await _store.GetUsersAsync();

        var response =
            users
                .Select(user => user.ToResponse())
                .ToList();

        return Ok(response);
    }

    // GET /api/users/{id}
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<UserResponse>>
        GetById(Guid id)
    {
        var user =
            await _store.GetUserByIdAsync(id);

        // The controller no longer creates a 404 ProblemDetails response.
        // It throws an exception which is handled centrally.
        if (user is null)
        {
            throw new ResourceNotFoundException(
                "The user was not found.");
        }

        return Ok(user.ToResponse());
    }

    // POST /api/users
    // FluentValidation checks the request before this action executes.
    [HttpPost]
    public async Task<ActionResult<UserResponse>>
        Create(CreateUserRequest request)
    {
        // Convert the request DTO into the User domain entity.
        var user =
            request.ToDomain();

        await _store.AddUserAsync(user);

        var response =
            user.ToResponse();

        // 201 Created and a link to the newly-created User.
        return CreatedAtAction(
            nameof(GetById),
            new { id = user.Id },
            response);
    }

    // PUT /api/users/{id}
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<UserResponse>>
        Update(
            Guid id,
            UpdateUserRequest request)
    {
        var user =
            await _store.GetUserByIdAsync(id);

        if (user is null)
        {
            throw new ResourceNotFoundException(
                "The user was not found.");
        }

        // Manual mapping applies the request values
        // to the existing domain entity.
        request.ApplyTo(user);

        await _store.UpdateUserAsync(user);

        return Ok(user.ToResponse());
    }

    // DELETE /api/users/{id}
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult>
        Delete(Guid id)
    {
       await _service.DeleteUserAsync(id);

        return NoContent();
    }
}