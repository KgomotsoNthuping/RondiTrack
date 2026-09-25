using FluentValidation;
using Api.Contracts;

namespace Api.Validation;

public sealed class CreateStokvelRequestValidator : AbstractValidator<CreateStokvelRequest>
{
    public CreateStokvelRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Stokvel name is required.")
            .MaximumLength(50);

        RuleFor(x => x.MonthlyContribution)
            .GreaterThan(0)
            .WithMessage("Monthly contribution must be greater than zero.");

        RuleFor(x => x.TotalPeriods)
            .GreaterThan(0)
            .WithMessage("Total periods must be greater than zero.");

        RuleFor(x => x.CurrentPeriod)
            .GreaterThan(0)
            .WithMessage("Current period must be greater than zero.");

        RuleFor(x => x)
            .Must(x =>
                x.CurrentPeriod <= x.TotalPeriods)
            .When(x => x.TotalPeriods > 0)
            .WithMessage("Current period cannot be greater than total periods.");

        RuleFor(x => x.Rules)
            .MaximumLength(1000)
            .When(x => x.Rules is not null);
    }
}

public sealed class UpdateStokvelRequestValidator : AbstractValidator<UpdateStokvelRequest>
{
    public UpdateStokvelRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Stokvel name is required.")
            .MaximumLength(50);

        RuleFor(x => x.MonthlyContribution)
            .GreaterThan(0);

        RuleFor(x => x.TotalPeriods)
            .GreaterThan(0);

        RuleFor(x => x.CurrentPeriod)
            .GreaterThan(0);

        RuleFor(x => x)
            .Must(x =>
                x.CurrentPeriod <= x.TotalPeriods)
            .When(x => x.TotalPeriods > 0)
            .WithMessage("Current period cannot be greater than total periods.");
    }
}