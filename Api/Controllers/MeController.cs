using Api.Common;
using Api.DTO;
using Api.Mappings;
using Api.Services;
using Domain.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route(ApiRoutes.V1 + "/me")]
[Authorize]
public sealed class MeController : ControllerBase
{
    private readonly IMemberHomeService _homeService;
    private readonly ITrackStore _store;

    public MeController(IMemberHomeService homeService, ITrackStore store)
    {
        _homeService = homeService;
        _store = store;
    }

    // GET /api/v1/me
    // Returns the authenticated user's profile.
    [HttpGet]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<UserResponse>> GetMe()
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        var user = await _store.GetUserByIdAsync( userId, asNoTracking: true);

        if (user is null)
        {
            return NotFound();
        }

        return Ok(user.ToResponse());
    }

    // GET /api/v1/me/home
    // Returns only the signed-in member's stokvels.
    [HttpGet("home")]
    [ProducesResponseType(typeof(MemberHomeResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<MemberHomeResponse>> GetHome()
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        var response = await _homeService.GetAsync(userId);

        return Ok(response);
    }

    private bool TryGetUserId(out Guid userId)
    {
        var userIdValue = User.FindFirst("sub")?.Value;

        return Guid.TryParse(userIdValue, out userId);
    }
}
