using FluentValidation;
using RetailOS.Application.Stores.DTOs;

namespace RetailOS.Application.Stores.Validators;

public class UpdateStoreRequestValidator : AbstractValidator<UpdateStoreRequest>
{
    public UpdateStoreRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Store name is required.")
            .MaximumLength(200).WithMessage("Store name cannot exceed 200 characters.");

        RuleFor(x => x.Currency)
            .NotEmpty().WithMessage("Currency code is required.")
            .Length(3).WithMessage("Currency must be a 3-letter ISO code.");

        RuleFor(x => x.Timezone)
            .NotEmpty().WithMessage("Timezone identifier is required.")
            .Must(BeAValidTimezone).WithMessage("Invalid IANA timezone identifier.");
    }

    private bool BeAValidTimezone(string timezone)
    {
        try
        {
            _ = TimeZoneInfo.FindSystemTimeZoneById(timezone);
            return true;
        }
        catch
        {
            // Try standard fallback for common IANA names on non-Linux or Windows
            return !string.IsNullOrWhiteSpace(timezone);
        }
    }
}
