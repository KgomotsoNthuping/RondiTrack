using Domain.Models;
using Microsoft.AspNetCore.Authorization;

namespace Api.Auth;

public static class Policies
{
    public const string StokvelMember =
        "StokvelMember";

    public const string StokvelAdmin =
        "StokvelAdmin";
}

public enum StokvelAccessLevel
{
    Member,
    Admin
}

public sealed class StokvelAccessRequirement
    : IAuthorizationRequirement
{
    public StokvelAccessLevel AccessLevel { get; }

    public StokvelAccessRequirement(
        StokvelAccessLevel accessLevel)
    {
        AccessLevel = accessLevel;
    }
}

public sealed record StokvelResource(
    Guid StokvelId);

public static class StokvelAuthorizationFailureReasons
{
    public const string NotMember =
        "NotMember";

    public const string InsufficientRole =
        "InsufficientRole";
}