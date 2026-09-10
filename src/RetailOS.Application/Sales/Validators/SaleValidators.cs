using FluentValidation;
using RetailOS.Application.Sales.DTOs;
using RetailOS.Domain.Enums;

namespace RetailOS.Application.Sales.Validators;

public class CreateSaleRequestValidator : AbstractValidator<CreateSaleRequest>
{
    public CreateSaleRequestValidator()
    {
        RuleFor(x => x.PaymentMethod)
            .NotEmpty().WithMessage("PaymentMethod is required")
            .IsEnumName(typeof(SalePaymentMethod), caseSensitive: false)
            .WithMessage("PaymentMethod must be Cash, Credit, or Mixed");

        RuleFor(x => x.CustomerId)
            .NotEmpty().When(x => string.Equals(x.PaymentMethod, nameof(SalePaymentMethod.Credit), StringComparison.OrdinalIgnoreCase)
                               || string.Equals(x.PaymentMethod, nameof(SalePaymentMethod.Mixed), StringComparison.OrdinalIgnoreCase))
            .WithMessage("Customer selection is required for Credit or Mixed payments");

        RuleFor(x => x.CashAmount)
            .GreaterThan(0).When(x => string.Equals(x.PaymentMethod, nameof(SalePaymentMethod.Mixed), StringComparison.OrdinalIgnoreCase))
            .WithMessage("Cash amount must be greater than 0 for Mixed payments");

        RuleFor(x => x.CashAmount)
            .GreaterThanOrEqualTo(0).WithMessage("Cash amount cannot be negative");

        RuleFor(x => x.Items)
            .NotEmpty().WithMessage("Sale must contain at least one line item");

        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.ProductId).NotEmpty().WithMessage("ProductId is required");
            item.RuleFor(i => i.Quantity).GreaterThan(0).WithMessage("Quantity must be greater than 0");
            item.RuleFor(i => i.UnitPrice).GreaterThanOrEqualTo(0).WithMessage("UnitPrice must be greater than or equal to 0");
            item.RuleFor(i => i.Discount).GreaterThanOrEqualTo(0).WithMessage("Discount must be greater than or equal to 0");
        });
    }
}

public class CreateSaleReturnRequestValidator : AbstractValidator<CreateSaleReturnRequest>
{
    public CreateSaleReturnRequestValidator()
    {
        RuleFor(x => x.Reason)
            .NotEmpty().WithMessage("Return reason is required")
            .MaximumLength(500).WithMessage("Reason must not exceed 500 characters");

        RuleFor(x => x.RefundMethod)
            .NotEmpty().WithMessage("RefundMethod is required")
            .IsEnumName(typeof(RefundMethod), caseSensitive: false)
            .WithMessage("RefundMethod must be Cash or Credit");

        RuleFor(x => x.Items)
            .NotEmpty().WithMessage("Return must contain at least one line item");

        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.ProductId).NotEmpty().WithMessage("ProductId is required");
            item.RuleFor(i => i.Quantity).GreaterThan(0).WithMessage("Quantity must be greater than 0");
        });
    }
}
