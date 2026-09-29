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

    /// <summary>
    /// Returns all Users as response DTOs rather than exposing entities.
    /// </summary>
    /// <remarks>
    /// Returns the current collection of users.
    /// An empty collection is returned when no users exist.
    /// Entities are not exposed directly; UserResponse DTOs
    /// are returned instead.
    /// </remarks>
    /// <returns>The collection of users.</returns>
    /// <response code="200">
    /// Returns the collection of users.
    /// </response>
    // GET /api/users
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyCollection<UserResponse>),StatusCodes.Status200OK)]
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

    
    /// <summary>
    /// Gets a specific user.
    /// </summary>
    /// <remarks>
    /// Looks up a user using the supplied unique identifier.
    /// This endpoint does not create or modify any data.
    /// </remarks>
    /// <param name="id">
    /// The unique identifier of the user.
    /// </param>
    /// <returns>The requested user.</returns>
    /// <response code="200">
    /// Returns the requested user.
    /// </response>
    /// <response code="404">
    /// No user exists with the supplied identifier.
    /// </response>
    //GET /api/users/{id}
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(UserResponse),StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails),StatusCodes.Status404NotFound,"application/problem+json")]
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

    
    /// <summary>
    /// FluentValidation checks the request before this action executes.
    /// </summary>
    /// <remarks>
    /// Creates a user from the supplied request.
    /// The API generates values such as the user's identifier.
    /// The caller cannot provide those values.
    ///
    /// FluentValidation validates the request before the controller
    /// action executes.
    /// </remarks>
    /// <param name="request">
    /// The details required to create the user.
    /// </param>
    /// <returns>The newly created user.</returns>
    /// <response code="201">
    /// The user was successfully created.
    /// </response>
    /// <response code="400">
    /// The request failed input validation.
    /// </response>
    // POST /api/users
    [HttpPost]
    [ProducesResponseType(typeof(UserResponse),StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails),StatusCodes.Status400BadRequest,"application/problem+json")]
    public async Task<ActionResult<UserResponse>>
        Create( [FromBody] CreateUserRequest request)
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

    
    /// <summary>
    /// Updates an existing user.
    /// </summary>
    /// <remarks>
    /// Replaces the editable profile details of an existing user.
    /// The user's identifier and creation timestamp cannot be changed.
    /// </remarks>
    /// <param name="id">
    /// The unique identifier of the user being updated.
    /// </param>
    /// <param name="request">
    /// The replacement profile information.
    /// </param>
    /// <returns>The updated user.</returns>
    /// <response code="200">
    /// The user was successfully updated.
    /// </response>
    /// <response code="400">
    /// The request failed input validation.
    /// </response>
    /// <response code="404">
    /// The requested user does not exist.
    /// </response>
    // PUT /api/users/{id}
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(UserResponse),StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails),StatusCodes.Status400BadRequest,"application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails),StatusCodes.Status404NotFound,"application/problem+json")]
    public async Task<ActionResult<UserResponse>>
        Update(
            Guid id,
            [FromBody] UpdateUserRequest request)
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

    
    /// <summary>
    /// Deletes a user.
    /// </summary>
    /// <remarks>
    /// Deletes the requested user only when the user is not currently
    /// a member of a stokvel.
    ///
    /// Membership protection is a business rule and is enforced by
    /// the service layer.
    /// </remarks>
    /// <param name="id">
    /// The unique identifier of the user being deleted.
    /// </param>
    /// <response code="204">
    /// The user was successfully deleted.
    /// </response>
    /// <response code="404">
    /// The requested user does not exist.
    /// </response>
    /// <response code="409">
    /// The user still belongs to a stokvel and cannot be deleted.
    /// </response>
    // DELETE /api/users/{id}
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails),StatusCodes.Status404NotFound,"application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails),StatusCodes.Status409Conflict,"application/problem+json")]
    public async Task<IActionResult>
        Delete(Guid id)
    {
       await _service.DeleteUserAsync(id);

        return NoContent();
    }
}