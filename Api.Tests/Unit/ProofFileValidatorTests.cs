using System.Text;
using Api.Services;
using Domain.Exceptions;

namespace Api.Tests.Unit;

public sealed class ProofFileValidatorTests
{
    [Fact]
    public void Recognises_pdf_from_its_bytes()
    {
        var bytes = Encoding.ASCII.GetBytes(
            "%PDF-1.7\n1 0 obj\n<<>>\nendobj\n%%EOF");

        Assert.Equal(
            "application/pdf",
            ProofFileValidator.Validate(bytes));
    }

    [Fact]
    public void Recognises_png_from_its_bytes()
    {
        var bytes = CreatePng();

        Assert.Equal(
            "image/png",
            ProofFileValidator.Validate(bytes));
    }

    [Fact]
    public void Recognises_jpeg_from_its_bytes()
    {
        byte[] bytes =
        [
            0xFF, 0xD8, 0xFF, 0xE0,
            0x00, 0x10, 0xFF, 0xD9
        ];

        Assert.Equal(
            "image/jpeg",
            ProofFileValidator.Validate(bytes));
    }

    [Fact]
    public void Rejects_text_disguised_as_pdf()
    {
        var bytes = Encoding.UTF8.GetBytes(
            "This is a text file renamed as proof.pdf");

        var error = Assert.Throws<ValidationException>(
            () => ProofFileValidator.Validate(bytes));

        Assert.Contains(
            "file",
            error.Errors.Keys);
    }

    [Fact]
    public void Rejects_empty_files()
    {
        Assert.Throws<ValidationException>(
            () => ProofFileValidator.Validate([]));
    }

    [Fact]
    public void Rejects_files_over_five_mib()
    {
        var bytes = new byte[
            ProofFileValidator.MaxFileBytes + 1];

        Assert.Throws<ValidationException>(
            () => ProofFileValidator.Validate(bytes));
    }

    private static byte[] CreatePng()
    {
        var bytes = new byte[24];

        byte[] signature =
        [
            137, 80, 78, 71,
            13, 10, 26, 10
        ];

        signature.CopyTo(bytes, 0);
        "IHDR"u8.CopyTo(bytes.AsSpan(12));

        return bytes;
    }
}
