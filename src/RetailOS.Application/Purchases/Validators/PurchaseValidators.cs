using FluentValidation;
using RetailOS.Application.Purchases.DTOs;

namespace RetailOS.Application.Purchases.Validators;

public class CreatePurchaseRequestValidator : AbstractValidator<CreatePurchaseRequest>
{
    public CreatePurchaseRequestValidator()
    {
        RuleFor(x => x.SupplierId)
            .NotEmpty().WithMessage("SupplierId is required");

        RuleFor(x => x.Items)
            .NotEmpty().WithMessage("Purchase must contain at least one line item");

        RuleForEach(x => x.Items).SetValidator(new PurchaseLineItemRequestValidator());
    }
}

public class UpdatePurchaseRequestValidator : AbstractValidator<UpdatePurchaseRequest>
{
    public UpdatePurchaseRequestValidator()
    {
        RuleFor(x => x.Items)
            .NotEmpty().WithMessage("Purchase must contain at least one line item");

        RuleForEach(x => x.Items).SetValidator(new PurchaseLineItemRequestValidator());
    }
}

public class PurchaseLineItemRequestValidator : AbstractValidator<PurchaseLineItemRequest>
{
    public PurchaseLineItemRequestValidator()
    {
        RuleFor(x => x.ProductId)
            .NotEmpty().WithMessage("ProductId is required");

        RuleFor(x => x.Quantity)
            .GreaterThan(0).WithMessage("Quantity must be greater than 0");

        RuleFor(x => x.UnitCost)
            .GreaterThanOrEqualTo(0).WithMessage("UnitCost must be greater than or equal to 0");

        RuleFor(x => x.Discount)
            .GreaterThanOrEqualTo(0).WithMessage("Discount must be greater than or equal to 0");
    }
}

public class CreatePurchaseReturnRequestValidator : AbstractValidator<CreatePurchaseReturnRequest>
{
    public CreatePurchaseReturnRequestValidator()
    {
        RuleFor(x => x.Reason)
            .NotEmpty().WithMessage("Return reason is required")
            .MaximumLength(500).WithMessage("Reason must not exceed 500 characters");

        RuleFor(x => x.Items)
            .NotEmpty().WithMessage("Return must contain at least one line item");

        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.ProductId).NotEmpty().WithMessage("ProductId is required");
            item.RuleFor(i => i.Quantity).GreaterThan(0).WithMessage("Quantity must be greater than 0");
        });
    }
}
