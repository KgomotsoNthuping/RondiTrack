using Api.Common;
using Api.DTO;
using Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route(ApiRoutes.V1 + "/me")]
[Authorize]
public sealed class MeController
    : ControllerBase
{
    private readonly IMemberHomeService
        _homeService;

    public MeController(
        IMemberHomeService homeService)
    {
        _homeService = homeService;
    }

    [HttpGet("home")]
    public async Task<
        ActionResult<MemberHomeResponse>>
        GetHome()
    {
        var userIdValue =
            User.FindFirst("sub")?.Value;

        if (!Guid.TryParse(
                userIdValue,
                out var userId))
        {
            return Unauthorized();
        }

        var response =
            await _homeService.GetAsync(
                userId);

        return Ok(response);
    }
}