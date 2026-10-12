using Api.DTO;
using Api.Mappings;
using Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly AuthService _authService;

    public AuthController(AuthService authService)
    {
        _authService = authService;
    }

    [AllowAnonymous]
    [HttpPost("register")]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails),StatusCodes.Status400BadRequest,"application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails),StatusCodes.Status409Conflict, "application/problem+json")]
    public async Task<ActionResult<UserResponse>> Register([FromBody] RegisterRequest request)
    {
        var user = await _authService.RegisterAsync(request);

        return Created($"/api/users/{user.Id}", user.ToResponse());
    }
}