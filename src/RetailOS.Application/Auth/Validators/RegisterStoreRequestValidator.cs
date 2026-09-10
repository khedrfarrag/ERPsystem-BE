using FluentValidation;
using RetailOS.Application.Auth.DTOs;

namespace RetailOS.Application.Auth.Validators;

public class RegisterStoreRequestValidator : AbstractValidator<RegisterStoreRequest>
{
    public RegisterStoreRequestValidator()
    {
        RuleFor(x => x.StoreName)
            .NotEmpty().WithMessage("Store name is required.")
            .MaximumLength(200).WithMessage("Store name must not exceed 200 characters.");

        RuleFor(x => x.BusinessType)
            .NotEmpty().WithMessage("Business type is required.")
            .MaximumLength(100).WithMessage("Business type must not exceed 100 characters.");

        RuleFor(x => x.OwnerFirstName)
            .NotEmpty().WithMessage("Owner first name is required.")
            .MaximumLength(100).WithMessage("First name must not exceed 100 characters.");

        RuleFor(x => x.OwnerLastName)
            .NotEmpty().WithMessage("Owner last name is required.")
            .MaximumLength(100).WithMessage("Last name must not exceed 100 characters.");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("A valid email address is required.")
            .MaximumLength(256).WithMessage("Email must not exceed 256 characters.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required.")
            .MinimumLength(6).WithMessage("Password must be at least 6 characters long.");
    }
}
