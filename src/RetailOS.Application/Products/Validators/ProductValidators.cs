using FluentValidation;
using RetailOS.Application.Products.DTOs;

namespace RetailOS.Application.Products.Validators;

public class CreateProductRequestValidator : AbstractValidator<CreateProductRequest>
{
    public CreateProductRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Product name is required.")
            .MaximumLength(300).WithMessage("Product name cannot exceed 300 characters.");

        RuleFor(x => x.CategoryId)
            .NotEmpty().WithMessage("Category is required.");

        RuleFor(x => x.UnitId)
            .NotEmpty().WithMessage("Unit is required.");

        RuleFor(x => x.SellingPrice)
            .GreaterThan(0).WithMessage("Selling price must be greater than zero.");

        RuleFor(x => x.WholesalePrice)
            .GreaterThanOrEqualTo(0)
            .When(x => x.WholesalePrice.HasValue)
            .WithMessage("سعر الجملة لا يمكن أن يكون سالباً.");

        RuleFor(x => x.PurchaseCost)
            .GreaterThanOrEqualTo(0)
            .When(x => x.PurchaseCost.HasValue)
            .WithMessage("Purchase cost cannot be negative.");

        RuleFor(x => x.MinStockLevel)
            .GreaterThanOrEqualTo(0)
            .When(x => x.MinStockLevel.HasValue)
            .WithMessage("Minimum stock level cannot be negative.");

        RuleFor(x => x.Barcode)
            .MaximumLength(100)
            .When(x => !string.IsNullOrEmpty(x.Barcode))
            .WithMessage("Barcode cannot exceed 100 characters.");

        RuleFor(x => x.Description)
            .MaximumLength(2000)
            .When(x => !string.IsNullOrEmpty(x.Description))
            .WithMessage("Description cannot exceed 2000 characters.");

        RuleFor(x => x.ImageUrl)
            .MaximumLength(500)
            .When(x => !string.IsNullOrEmpty(x.ImageUrl))
            .WithMessage("Image URL cannot exceed 500 characters.");
    }
}

public class UpdateProductRequestValidator : AbstractValidator<UpdateProductRequest>
{
    public UpdateProductRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Product name is required.")
            .MaximumLength(300).WithMessage("Product name cannot exceed 300 characters.");

        RuleFor(x => x.CategoryId)
            .NotEmpty().WithMessage("Category is required.");

        RuleFor(x => x.UnitId)
            .NotEmpty().WithMessage("Unit is required.");

        RuleFor(x => x.SellingPrice)
            .GreaterThan(0).WithMessage("Selling price must be greater than zero.");

        RuleFor(x => x.WholesalePrice)
            .GreaterThanOrEqualTo(0)
            .When(x => x.WholesalePrice.HasValue)
            .WithMessage("سعر الجملة لا يمكن أن يكون سالباً.");

        RuleFor(x => x.PurchaseCost)
            .GreaterThanOrEqualTo(0)
            .When(x => x.PurchaseCost.HasValue)
            .WithMessage("Purchase cost cannot be negative.");

        RuleFor(x => x.MinStockLevel)
            .GreaterThanOrEqualTo(0)
            .When(x => x.MinStockLevel.HasValue)
            .WithMessage("Minimum stock level cannot be negative.");

        RuleFor(x => x.Barcode)
            .MaximumLength(100)
            .When(x => !string.IsNullOrEmpty(x.Barcode))
            .WithMessage("Barcode cannot exceed 100 characters.");

        RuleFor(x => x.Description)
            .MaximumLength(2000)
            .When(x => !string.IsNullOrEmpty(x.Description))
            .WithMessage("Description cannot exceed 2000 characters.");

        RuleFor(x => x.ImageUrl)
            .MaximumLength(500)
            .When(x => !string.IsNullOrEmpty(x.ImageUrl))
            .WithMessage("Image URL cannot exceed 500 characters.");
    }
}
