using Domain.Models;
using Domain.Repositories;
using Microsoft.AspNetCore.Authorization;

namespace Api.Auth;

public class StokvelAuthorizationHandler : AuthorizationHandler<StokvelAccessRequirement, StokvelResource>
{
    private readonly ITrackStore _store;

    public StokvelAuthorizationHandler(ITrackStore store)
    {
        _store = store;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        StokvelAccessRequirement requirement,
        StokvelResource resource)
    {
        var userIdValue = context.User.FindFirst("sub")?.Value;

        if (!Guid.TryParse(userIdValue, out var userId))
        {
            context.Fail();
            return;
        }

        var membership = await _store.GetStokvelMemberAsync(
                resource.StokvelId,
                userId);

        if (membership is null)
        {
            context.Fail(
                new AuthorizationFailureReason(
                    this,
                    StokvelAuthorizationFailureReasons
                        .NotMember));

            return;
        }

        if (requirement.AccessLevel ==
            StokvelAccessLevel.Member)
        {
            context.Succeed(requirement);
            return;
        }

        if (membership.Role ==
            StokvelMemberRole.Admin)
        {
            context.Succeed(requirement);
            return;
        }

        context.Fail(
            new AuthorizationFailureReason(
                this,
                StokvelAuthorizationFailureReasons
                    .InsufficientRole));
    }
}