using System.Text.Json.Serialization;

namespace Api.Domain;

public sealed class User
{
    public Guid Id { get; private set; }

    public string FullName { get; private set; }

    public string Email { get; private set; }

    public string PhoneNumber { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    [JsonConstructor]
    public User(
        string fullName,
        string email,
        string phoneNumber)
    {
        Id = Guid.NewGuid();

        FullName = ValidateName(fullName);
        Email = ValidateEmail(email);
        PhoneNumber = ValidatePhoneNumber(phoneNumber);

        CreatedAtUtc = DateTime.UtcNow;
    }

    public void UpdateProfile(
        string fullName,
        string email,
        string phoneNumber)
    {
        FullName = ValidateName(fullName);
        Email = ValidateEmail(email);
        PhoneNumber = ValidatePhoneNumber(phoneNumber);
    }

    private static string ValidateName(string fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName))
        {
            throw new ArgumentException(
                "A user must have a full name.");
        }

        return fullName.Trim();
    }

    private static string ValidateEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email) ||
            !email.Contains('@'))
        {
            throw new ArgumentException(
                "A valid email address is required.");
        }

        return email.Trim().ToLowerInvariant();
    }

    private static string ValidatePhoneNumber(string phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
        {
            throw new ArgumentException(
                "A phone number is required.");
        }

        var digits =
            new string(phoneNumber.Where(char.IsDigit).ToArray());

        if (digits.Length < 9)
        {
            throw new ArgumentException(
                "The phone number is too short.");
        }

        return phoneNumber.Trim();
    }
}