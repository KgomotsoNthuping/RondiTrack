using Api.DTO;
using FluentValidation;

namespace Api.Validation;

public sealed class RecordCycleContributionRequestValidator
    : AbstractValidator<RecordCycleContributionRequest>
{
    public RecordCycleContributionRequestValidator()
    {
        RuleFor(request => request.UserId)
            .NotEmpty()
            .WithMessage("Select a member.");

        RuleFor(request => request.AmountInCents)
            .GreaterThan(0)
            .WithMessage("Enter an amount greater than zero.");

        RuleFor(request => request.AmountInCents)
            .LessThanOrEqualTo(9007199254740991L)
            .WithMessage("The contribution amount is too large.");
    }
}
