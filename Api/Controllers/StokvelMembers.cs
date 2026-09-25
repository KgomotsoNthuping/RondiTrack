using Microsoft.AspNetCore.Mvc;
using Api.DTO;
using Api.Data;
using Api.Extensions;
using Api.Mappings;
using Api.Services;

namespace Api.Controllers;

[ApiController]
[Route("api/stokvels/{stokvelId:guid}/members")]
public sealed class StokvelMember : ControllerBase
{
    private readonly ITrackStore _store;
    private readonly ITrackService _service;

    public StokvelMember(
        ITrackStore store,
        ITrackService service)
    {
        _store = store;
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<UserResponse>>>
        GetMembers(Guid stokvelId)
    {
        var stokvel =
            await _store.GetStokvelByIdAsync(stokvelId);

        if (stokvel is null)
        {
            return this.NotFoundProblem(
                "The stokvel was not found.");
        }

        var response =
            stokvel.Members
                .Select(user => user.ToResponse())
                .ToList();

        return Ok(response);
    }

    [HttpGet("{userId:guid}")]
    public async Task<ActionResult<UserResponse>>
        GetMember(
            Guid stokvelId,
            Guid userId)
    {
        var stokvel =
            await _store.GetStokvelByIdAsync(stokvelId);

        if (stokvel is null)
        {
            return this.NotFoundProblem(
                "The stokvel was not found.");
        }

        var member =
            stokvel.Members.FirstOrDefault(
                user => user.Id == userId);

        if (member is null)
        {
            return this.NotFoundProblem(
                "The member was not found in this stokvel.");
        }

        return Ok(member.ToResponse());
    }

    [HttpPost("{userId:guid}")]
    public async Task<ActionResult<UserResponse>>
        AddMember(
            Guid stokvelId,
            Guid userId)
    {
        var user =
            await _service.AddMemberAsync(
                stokvelId,
                userId);

        return CreatedAtAction(
        nameof(GetMember),
        new
        {
            stokvelId,
            userId
        },
        user.ToResponse());
    }

    [HttpDelete("{userId:guid}")]
    public async Task<IActionResult>
        RemoveMember(
            Guid stokvelId,
            Guid userId)
    {
        var result =
            await _service.RemoveMemberAsync(
                stokvelId,
                userId);

        if (!result.IsSuccess)
        {
            return this.ToProblem(result);
        }

        return NoContent();
    }
}