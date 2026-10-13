using Domain.Models;
using Domain.Repositories;
using Microsoft.AspNetCore.Authorization;
using Api.Observability;

namespace Api.Auth;

public class StokvelAuthorizationHandler : AuthorizationHandler<StokvelAccessRequirement, StokvelResource>
{
    private readonly ITrackStore _store;
    private readonly Metrics _metrics;

    public StokvelAuthorizationHandler(ITrackStore store, Metrics metrics)
    {
        _store = store;
        _metrics = metrics;
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
            _metrics.AuthorizationNotFound();

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

        _metrics.AuthorizationForbidden();
        
        context.Fail(
            new AuthorizationFailureReason(
                this,
                StokvelAuthorizationFailureReasons
                    .InsufficientRole));
    }
}