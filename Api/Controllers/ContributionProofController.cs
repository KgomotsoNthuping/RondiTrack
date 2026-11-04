using Api.Common;
using Api.DTO;
using Api.Services;
using Domain.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Authorize]
[Route(
    ApiRoutes.V1 +
    "/stokvels/{stokvelId:guid}/contributions/{contributionId:guid}/proof")]
public sealed class ContributionProofController : ControllerBase
{
    private readonly ContributionProofService _service;

    public ContributionProofController(
        ContributionProofService service)
    {
        _service = service;
    }

    [HttpPut]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(6_291_456)]
    [RequestFormLimits(
        MultipartBodyLengthLimit = 6_291_456)]
    public async Task<ActionResult<ContributionProofResponse>>
        Upload(
            Guid stokvelId,
            Guid contributionId,
            [FromForm] IFormFile? file,
            CancellationToken cancellationToken)
    {
        if (!TryGetCallerId(out var callerId))
        {
            return Unauthorized();
        }

        if (file is null)
        {
            throw new ValidationException(
                new Dictionary<string, string[]>
                {
                    ["file"] =
                    [
                        "Choose a proof-of-payment file."
                    ]
                });
        }

        await using var input = file.OpenReadStream();

        var result = await _service.UploadAsync(
            stokvelId,
            contributionId,
            callerId,
            input,
            file.Length,
            cancellationToken);

        return Ok(result);
    }

    [HttpGet("metadata")]
    public async Task<ActionResult<ContributionProofResponse>>
        GetMetadata(
            Guid stokvelId,
            Guid contributionId,
            CancellationToken cancellationToken)
    {
        if (!TryGetCallerId(out var callerId))
        {
            return Unauthorized();
        }

        var result = await _service.GetMetadataAsync(
            stokvelId,
            contributionId,
            callerId,
            cancellationToken);

        Response.Headers.CacheControl =
            "private, no-store";

        return Ok(result);
    }

    [HttpGet]
    public async Task<IActionResult> Download(
        Guid stokvelId,
        Guid contributionId,
        CancellationToken cancellationToken)
    {
        if (!TryGetCallerId(out var callerId))
        {
            return Unauthorized();
        }

        var result = await _service.DownloadAsync(
            stokvelId,
            contributionId,
            callerId,
            cancellationToken);

        Response.Headers.CacheControl =
            "private, no-store";

        Response.Headers.XContentTypeOptions =
            "nosniff";

        return File(
            result.Content,
            result.ContentType,
            result.FileName);
    }

    [HttpDelete]
    public async Task<IActionResult> Delete(
        Guid stokvelId,
        Guid contributionId,
        CancellationToken cancellationToken)
    {
        if (!TryGetCallerId(out var callerId))
        {
            return Unauthorized();
        }

        await _service.DeleteAsync(
            stokvelId,
            contributionId,
            callerId,
            cancellationToken);

        return NoContent();
    }

    private bool TryGetCallerId(out Guid userId)
    {
        return Guid.TryParse(
            User.FindFirst("sub")?.Value,
            out userId);
    }
}
