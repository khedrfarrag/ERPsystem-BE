using FluentValidation;
using RetailOS.Application.Units.DTOs;

namespace RetailOS.Application.Units.Validators;

public class CreateUnitRequestValidator : AbstractValidator<CreateUnitRequest>
{
    public CreateUnitRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Unit name is required.")
            .MaximumLength(100).WithMessage("Unit name cannot exceed 100 characters.");

        RuleFor(x => x.Symbol)
            .NotEmpty().WithMessage("Unit symbol is required.")
            .MaximumLength(20).WithMessage("Unit symbol cannot exceed 20 characters.");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("Description cannot exceed 500 characters.");
    }
}

public class UpdateUnitRequestValidator : AbstractValidator<UpdateUnitRequest>
{
    public UpdateUnitRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Unit name is required.")
            .MaximumLength(100).WithMessage("Unit name cannot exceed 100 characters.");

        RuleFor(x => x.Symbol)
            .NotEmpty().WithMessage("Unit symbol is required.")
            .MaximumLength(20).WithMessage("Unit symbol cannot exceed 20 characters.");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("Description cannot exceed 500 characters.");
    }
}
