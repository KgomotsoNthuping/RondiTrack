using FluentValidation;
using Api.Contracts;

namespace Api.Validation;

public sealed class RecordContributionRequestValidator : AbstractValidator<RecordContributionRequest>
{
    public RecordContributionRequestValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("User ID is required.");

        RuleFor(x => x.ContributionCycleId)
            .NotEmpty()
            .WithMessage("Contribution cycle ID is required.");

        RuleFor(x => x.Amount)
            .GreaterThan(0)
            .WithMessage("Contribution amount must be greater than zero.");
    }
}