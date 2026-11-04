using Api.Common;
using Api.DTO;
using Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Api.Controllers;

[ApiController]
[Authorize]
[Route(ApiRoutes.V1 + "/invites")]
public sealed class InvitesController : ControllerBase
{
    private readonly StokvelInviteService _service;

    public InvitesController(
        StokvelInviteService service)
    {
        _service = service;
    }

    [HttpGet("{code}")]
    [EnableRateLimiting(RateLimitPolicies.Invite)]
    public async Task<
        ActionResult<StokvelInvitePreviewResponse>>
        Preview(string code)
    {
        if (!Guid.TryParse(
            User.FindFirst("sub")?.Value,
            out var userId))
        {
            return Unauthorized();
        }

        var result = await _service.PreviewAsync(
            code,
            userId);

        return Ok(result);
    }

    [HttpPost("{code}/join")]
    [EnableRateLimiting(RateLimitPolicies.Invite)]
    public async Task<
        ActionResult<JoinStokvelResponse>>
        Join(string code)
    {
        if (!Guid.TryParse(
            User.FindFirst("sub")?.Value,
            out var userId))
        {
            return Unauthorized();
        }

        var result = await _service.JoinAsync(
            code,
            userId);

        return Ok(result);
    }
}
