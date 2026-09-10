using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RetailOS.Domain.Entities;

namespace RetailOS.Infrastructure.Persistence.Configurations;

public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("products");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Name)
            .IsRequired()
            .HasMaxLength(300);

        builder.Property(p => p.Barcode)
            .HasMaxLength(100);

        builder.Property(p => p.Description)
            .HasMaxLength(2000);

        builder.Property(p => p.SellingPrice)
            .IsRequired()
            .HasPrecision(19, 4);

        builder.Property(p => p.WholesalePrice)
            .HasPrecision(19, 4);

        builder.Property(p => p.IsWholesaleAvailable)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(p => p.PurchaseCost)
            .HasPrecision(19, 4);

        builder.Property(p => p.MinStockLevel)
            .HasPrecision(19, 4);

        builder.Property(p => p.ImageUrl)
            .HasMaxLength(500);

        builder.Property(p => p.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.HasIndex(p => p.StoreId);
        builder.HasIndex(p => new { p.StoreId, p.CategoryId });
        builder.HasIndex(p => new { p.StoreId, p.IsActive });
        builder.HasIndex(p => new { p.StoreId, p.IsWholesaleAvailable });

        builder.HasOne(p => p.Category)
            .WithMany(c => c.Products)
            .HasForeignKey(p => p.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.Unit)
            .WithMany(u => u.Products)
            .HasForeignKey(p => p.UnitId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Store>()
            .WithMany()
            .HasForeignKey(p => p.StoreId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
