using Microsoft.AspNetCore.Mvc;
using Api.DTO;
using Api.Data;
using Api.Extensions;
using Api.Mappings;
using Api.Services;
using Domain.Repositories;

namespace Api.Controllers;

[ApiController]
[Route("api/stokvels/{stokvelId:guid}/members")]
public sealed class StokvelMember : ControllerBase
{
    private readonly ITrackStore _store;
    private readonly ITrackService _service;
    private readonly IStokvelMemberQueryService _memberQueryService;
    private readonly StokvelAccessAuthorizer _access;

    public StokvelMember(
        ITrackStore store,
        ITrackService service,
        IStokvelMemberQueryService memberQueryService,
        StokvelAccessAuthorizer access)
    {
        _store = store;
        _service = service;
        _memberQueryService = memberQueryService;
        _access = access;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResponse<UserResponse>>> GetMembers(
            Guid stokvelId, [FromQuery] StokvelMemberPageRequest request)
    {
        if (!await _access.AuthorizeAsync(User, stokvelId, Policies.StokvelMember))
        {
            return Forbid();
        }
        var members = await _memberQueryService.GetMembersAsync(stokvelId, request);

        return Ok(members);
    }

    [HttpGet("{userId:guid}")]
    public async Task<ActionResult<UserResponse>> GetMember(Guid stokvelId, Guid userId)
    {
        if (!await _access.AuthorizeAsync(User, stokvelId, Policies.StokvelMember))
        {
            return Forbid();
        }
        var stokvel = await _store.GetStokvelByIdAsync(stokvelId, asNoTracking: true);

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
        if (!await _access.AuthorizeAsync(
        User,
        stokvelId,
        Policies.StokvelAdmin))
        {
            return Forbid();
        }
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
        if (!await _access.AuthorizeAsync(
        User,
        stokvelId,
        Policies.StokvelAdmin))
        {
            return Forbid();
        }
        await _service.RemoveMemberAsync(
            stokvelId,
            userId);

        return NoContent();
    }
}