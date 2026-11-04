using Domain.Exceptions;

namespace Api.Services;

public static class ProofFileValidator
{
    public const int MaxFileBytes = 5 * 1024 * 1024;

    public static string Validate(
        ReadOnlySpan<byte> data)
    {
        if (data.IsEmpty)
        {
            throw Invalid(
                "Choose a non-empty PDF, JPEG or PNG file.");
        }

        if (data.Length > MaxFileBytes)
        {
            throw Invalid(
                "The file must not exceed 5 MiB.");
        }

        if (
            data.Length >= 8 &&
            data.StartsWith("%PDF-"u8)
        )
        {
            return "application/pdf";
        }

        if (
            data.Length >= 24 &&
            data.StartsWith(
                new byte[]
                {
                    137, 80, 78, 71,
                    13, 10, 26, 10
                }) &&
            data.Slice(12, 4)
                .SequenceEqual("IHDR"u8)
        )
        {
            return "image/png";
        }

        if (
            data.Length >= 4 &&
            data[0] == 0xFF &&
            data[1] == 0xD8 &&
            data[2] == 0xFF &&
            data[^2] == 0xFF &&
            data[^1] == 0xD9
        )
        {
            return "image/jpeg";
        }

        throw Invalid(
            "Only genuine PDF, JPEG and PNG files are accepted.");
    }

    private static ValidationException Invalid(
        string message)
    {
        return new ValidationException(
            new Dictionary<string, string[]>
            {
                ["file"] = [message]
            });
    }
}
