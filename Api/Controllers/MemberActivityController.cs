using Api.Common;
using Api.DTO;
using Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Authorize]
[Route(ApiRoutes.V1 + "/me/activity")]
public sealed class MemberActivityController
    : ControllerBase
{
    private readonly MemberActivityService _service;

    public MemberActivityController(
        MemberActivityService service)
    {
        _service = service;
    }

    [HttpGet]
    [ProducesResponseType(
        typeof(PagedResponse<MemberActivityItemResponse>),
        StatusCodes.Status200OK)]
    public async Task<
        ActionResult<PagedResponse<MemberActivityItemResponse>>>
        Get(
            [FromQuery] int pageSize = 10,
            [FromQuery] string? pageToken = null)
    {
        var userIdText =
            User.FindFirst("sub")?.Value;

        if (!Guid.TryParse(
            userIdText,
            out var userId))
        {
            return Unauthorized();
        }

        var result = await _service.GetAsync(
            userId,
            pageSize,
            pageToken);

        return Ok(result);
    }
}
