namespace Api.DTO;

/// <summary>
/// Information the client is allowed to send when creating a User.
/// </summary>
/// <example>
///   "fullName": "Kgomotso Nthuping",
///   "email": "kgomotso@rondi.co.za",
///   "phoneNumber": "0741111111"
/// </example>
public sealed class CreateUserRequest
{
    public string? FullName { get; init; }

    public string? Email { get; init; }

    public string? PhoneNumber { get; init; }
}

/// <summary>
/// Information the client is allowed to update
/// </summary>
/// <example>
///   "fullName": "Kgomotso Nthuping",
///   "email": "kgomotso.nthuping@rondi.co.za",
///   "phoneNumber": "0742222222"
/// </example>
public sealed class UpdateUserRequest
{
    public string? FullName { get; init; }

    public string? Email { get; init; }

    public string? PhoneNumber { get; init; }
}

/// <summary>
/// Shape/representation returned to API user.
/// </summary>
/// <example>
///   "id": "11111111-1111-1111-1111-111111111111",
///   "fullName": "Kgomotso Nthuping",
///   "email": "kgomotso@rondi.co.za",
///   "phoneNumber": "07411111111"
/// </example>
public sealed record UserResponse(
    Guid Id,
    string FullName,
    string Email,
    string PhoneNumber);