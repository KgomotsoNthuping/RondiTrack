using Api.DTO;
using FluentValidation;

namespace Api.Validation;

public sealed class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    public RegisterRequestValidator()
    {
        RuleFor(request => request.FullName)
            .NotEmpty();

        RuleFor(request => request.Email)
            .NotEmpty()
            .EmailAddress();

        RuleFor(request => request.PhoneNumber)
            .NotEmpty();

        RuleFor(request => request.Password)
            .NotEmpty()
            .MinimumLength(12)
            .WithMessage("Password must be at least 12 characters.");
    }
}