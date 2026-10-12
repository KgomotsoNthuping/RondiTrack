namespace Api.DTO;

public sealed class RegisterRequest
{
    public string? FullName { get; init; }

    public string? Email { get; init; }

    public string? PhoneNumber { get; init; }

    public string? Password { get; init; }
}