using Domain.Models;
using Domain.Repositories;
using Microsoft.AspNetCore.Authorization;
using Api.Observability;

namespace Api.Auth;

public class StokvelAuthorizationHandler : AuthorizationHandler<StokvelAccessRequirement, StokvelResource>
{
    private readonly ITrackStore _store;
    private readonly Metrics _metrics;
    private readonly ILogger<StokvelAuthorizationHandler> _logger;

    public StokvelAuthorizationHandler(ITrackStore store, Metrics metrics, ILogger<StokvelAuthorizationHandler> logger)
    {
        _store = store;
        _metrics = metrics;
        _logger = logger;
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
            _logger.LogInformation("Stokvel authorization refused for user {UserId} with result {Result}",userId,"not_found");

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
        _logger.LogInformation("Stokvel authorization refused for user {UserId} with result {Result}", userId,"forbidden");
        
        context.Fail(
            new AuthorizationFailureReason(
                this,
                StokvelAuthorizationFailureReasons
                    .InsufficientRole));
    }
}