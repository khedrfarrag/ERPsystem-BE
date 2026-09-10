using FluentValidation;
using RetailOS.Application.CashRegister.DTOs;

namespace RetailOS.Application.CashRegister.Validators;

public class OpenFloatRequestValidator : AbstractValidator<OpenFloatRequest>
{
    public OpenFloatRequestValidator()
    {
        RuleFor(x => x.Amount)
            .GreaterThanOrEqualTo(0).WithMessage("Float amount must be greater than or equal to 0");

        RuleFor(x => x.Notes)
            .MaximumLength(1000).WithMessage("Notes must not exceed 1000 characters");
    }
}

public class CloseRegisterRequestValidator : AbstractValidator<CloseRegisterRequest>
{
    public CloseRegisterRequestValidator()
    {
        RuleFor(x => x.CountedAmount)
            .GreaterThanOrEqualTo(0).WithMessage("Counted amount must be greater than or equal to 0");

        RuleFor(x => x.Notes)
            .MaximumLength(1000).WithMessage("Notes must not exceed 1000 characters");
    }
}
