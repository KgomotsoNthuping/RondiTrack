using System.Security.Claims;
using Domain.Exceptions;
using Microsoft.AspNetCore.Authorization;

namespace Api.Auth;

public sealed class StokvelAccessAuthorizer
{
    private readonly IAuthorizationService
        _authorizationService;

    public StokvelAccessAuthorizer(
        IAuthorizationService authorizationService)
    {
        _authorizationService =
            authorizationService;
    }

    public async Task<bool> AuthorizeAsync(
        ClaimsPrincipal user,
        Guid stokvelId,
        string policy)
    {
        var result =
            await _authorizationService
                .AuthorizeAsync(
                    user,
                    new StokvelResource(stokvelId),
                    policy);

        if (result.Succeeded)
        {
            return true;
        }

        var notMember =
            result.Failure?
                .FailureReasons
                .Any(reason =>
                    reason.Message ==
                    StokvelAuthorizationFailureReasons
                        .NotMember)
            == true;

        if (notMember)
        {
            throw new ResourceNotFoundException(
                "The stokvel was not found.");
        }

        return false;
    }
}