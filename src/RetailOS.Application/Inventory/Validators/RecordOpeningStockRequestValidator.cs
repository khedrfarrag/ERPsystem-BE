using FluentValidation;
using RetailOS.Application.Inventory.DTOs;

namespace RetailOS.Application.Inventory.Validators;

public class RecordOpeningStockRequestValidator : AbstractValidator<RecordOpeningStockRequest>
{
    public RecordOpeningStockRequestValidator()
    {
        RuleFor(x => x.ProductId)
            .NotEmpty().WithMessage("Product ID is required.");

        RuleFor(x => x.Quantity)
            .GreaterThan(0).WithMessage("Quantity must be greater than zero.");

        RuleFor(x => x.CostPerUnit)
            .GreaterThanOrEqualTo(0).WithMessage("Cost per unit cannot be negative.");
    }
}
