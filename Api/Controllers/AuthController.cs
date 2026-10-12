using Api.DTO;
using Api.Mappings;
using Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Domain.Exceptions;

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

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest request)
    {
        var result = await _authService.LoginAsync(request);

        SetRefreshCookie(result);

        return Ok(new AuthResponse(result.AccessToken, result.AccessTokenExpiresAtUtc));
    }

    [AllowAnonymous]
    [HttpPost("refresh")]
    public async Task<ActionResult<AuthResponse>> Refresh()
    {
        if (!Request.Cookies.TryGetValue("refresh_token", out var refreshToken))
        {
            throw new AuthenticationFailedException("Refresh token is invalid.");
        }

        var result = await _authService.RefreshAsync(refreshToken);

        SetRefreshCookie(result);

        return Ok(new AuthResponse(result.AccessToken, result.AccessTokenExpiresAtUtc));
    }

    private void SetRefreshCookie(AuthResult result)
    {
        Response.Cookies.Append("refresh_token", result.RefreshToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Path = "/api/auth",
                Expires = result.RefreshTokenExpiresAtUtc
            });
    }
}