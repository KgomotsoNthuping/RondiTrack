using Api.Common;
using Api.DTO;
using Api.Services;
using Api.Validation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Authorize]
[Route(
    ApiRoutes.V1 +
    "/stokvels/{stokvelId:guid}/cycles/{cycleId:guid}")]
public sealed class CycleCloseController : ControllerBase
{
    private readonly ManualCycleCloseService _service;

    public CycleCloseController(
        ManualCycleCloseService service)
    {
        _service = service;
    }

    [HttpGet("close-preview")]
    public async Task<ActionResult<CycleClosePreviewResponse>>
        Preview(
            Guid stokvelId,
            Guid cycleId,
            CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        var preview = await _service.PreviewAsync(
            stokvelId,
            cycleId,
            userId,
            cancellationToken);

        Response.Headers.CacheControl =
            "private, no-store";

        return Ok(preview);
    }

    [HttpPost("close")]
    [RequireIdempotencyKey]
    public async Task<ActionResult<CloseCycleResponse>>
        Close(
            Guid stokvelId,
            Guid cycleId,
            [FromHeader(Name = "Idempotency-Key")]
            string idempotencyKey,
            [FromBody] CloseCycleRequest request,
            CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        var result = await _service.CloseAsync(
            stokvelId,
            cycleId,
            userId,
            idempotencyKey,
            request.PreviewVersion,
            cancellationToken);

        Response.Headers.CacheControl =
            "private, no-store";

        return Ok(result);
    }

    private bool TryGetUserId(out Guid userId)
    {
        return Guid.TryParse(
            User.FindFirst("sub")?.Value,
            out userId);
    }
}
