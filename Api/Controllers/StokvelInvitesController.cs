using Api.Common;
using Api.DTO;
using Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Api.Controllers;

[ApiController]
[Authorize]
[Route(ApiRoutes.V1 + "/stokvels/{stokvelId:guid}/invites")]
public sealed class StokvelInvitesController : ControllerBase
{
    private readonly StokvelInviteService _service;

    public StokvelInvitesController(
        StokvelInviteService service)
    {
        _service = service;
    }

    [HttpPost]
    [EnableRateLimiting(RateLimitPolicies.Invite)]
    public async Task<ActionResult<CreateStokvelInviteResponse>>
        Create(Guid stokvelId)
    {
        if (!Guid.TryParse(
            User.FindFirst("sub")?.Value,
            out var userId))
        {
            return Unauthorized();
        }

        var result = await _service.CreateAsync(
            stokvelId,
            userId);

        return Created(
            $"/api/v1/invites/{result.Code}",
            result);
    }
}
