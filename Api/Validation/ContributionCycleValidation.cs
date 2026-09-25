using FluentValidation;
using Api.Contracts;

namespace Api.Validation;

public sealed class CreateContributionCycleRequestValidator : AbstractValidator<CreateContributionCycleRequest>
{
    public CreateContributionCycleRequestValidator()
    {
        RuleFor(x => x.CycleNumber)
            .GreaterThan(0)
            .WithMessage("Cycle number must be greater than zero.");

        RuleFor(x => x.TargetAmount)
            .GreaterThan(0)
            .WithMessage("Target amount must be greater than zero.");
    }
}

public sealed class UpdateContributionCycleRequestValidator : AbstractValidator<UpdateContributionCycleRequest>
{
    public UpdateContributionCycleRequestValidator()
    {
        RuleFor(x => x.CycleNumber)
            .GreaterThan(0)
            .WithMessage("Cycle number must be greater than zero.");

        RuleFor(x => x.TargetAmount)
            .GreaterThan(0)
            .WithMessage("Target amount must be greater than zero.");
    }
}