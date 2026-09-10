using FluentValidation;
using RetailOS.Application.Payments.DTOs;
using RetailOS.Domain.Enums;

namespace RetailOS.Application.Payments.Validators;

public class CreatePaymentRequestValidator : AbstractValidator<CreatePaymentRequest>
{
    public CreatePaymentRequestValidator()
    {
        RuleFor(x => x.PartyType)
            .NotEmpty().WithMessage("PartyType is required")
            .IsEnumName(typeof(PaymentPartyType), caseSensitive: false)
            .WithMessage("PartyType must be Customer or Supplier");

        RuleFor(x => x.Amount)
            .GreaterThan(0).WithMessage("Amount must be greater than 0");

        RuleFor(x => x.PaymentMethod)
            .NotEmpty().WithMessage("PaymentMethod is required")
            .IsEnumName(typeof(PaymentMethod), caseSensitive: false)
            .WithMessage("PaymentMethod must be Cash or BankTransfer");

        RuleFor(x => x.CustomerId)
            .NotEmpty().When(x => string.Equals(x.PartyType, nameof(PaymentPartyType.Customer), StringComparison.OrdinalIgnoreCase))
            .WithMessage("CustomerId is required when PartyType is Customer");

        RuleFor(x => x.SupplierId)
            .NotEmpty().When(x => string.Equals(x.PartyType, nameof(PaymentPartyType.Supplier), StringComparison.OrdinalIgnoreCase))
            .WithMessage("SupplierId is required when PartyType is Supplier");

        RuleFor(x => x.ReferenceNumber)
            .MaximumLength(100).WithMessage("ReferenceNumber must not exceed 100 characters");

        RuleFor(x => x.Notes)
            .MaximumLength(1000).WithMessage("Notes must not exceed 1000 characters");
    }
}
