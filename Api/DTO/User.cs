namespace Api.DTO;

// Information the client is allowed to send when creating a User.
public sealed class CreateUserRequest
{
    public string? FullName { get; init; }

    public string? Email { get; init; }

    public string? PhoneNumber { get; init; }
}

// Information the client is allowed to update.
public sealed class UpdateUserRequest
{
    public string? FullName { get; init; }

    public string? Email { get; init; }

    public string? PhoneNumber { get; init; }
}

// Shape returned to API clients.
public sealed record UserResponse(
    Guid Id,
    string FullName,
    string Email,
    string PhoneNumber);