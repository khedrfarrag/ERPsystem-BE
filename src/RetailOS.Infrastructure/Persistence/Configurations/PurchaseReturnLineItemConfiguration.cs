using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RetailOS.Domain.Entities;

namespace RetailOS.Infrastructure.Persistence.Configurations;

public class PurchaseReturnLineItemConfiguration : IEntityTypeConfiguration<PurchaseReturnLineItem>
{
    public void Configure(EntityTypeBuilder<PurchaseReturnLineItem> builder)
    {
        builder.ToTable("purchase_return_line_items");

        builder.HasKey(li => li.Id);

        builder.Property(li => li.Quantity)
            .IsRequired()
            .HasPrecision(19, 4);

        builder.Property(li => li.UnitCost)
            .IsRequired()
            .HasPrecision(19, 6);

        builder.Property(li => li.SubTotal)
            .IsRequired()
            .HasPrecision(19, 4);

        builder.HasIndex(li => li.StoreId);
        builder.HasIndex(li => li.PurchaseReturnId);
        builder.HasIndex(li => li.ProductId);

        builder.HasOne(li => li.PurchaseReturn)
            .WithMany(pr => pr.LineItems)
            .HasForeignKey(li => li.PurchaseReturnId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(li => li.Product)
            .WithMany()
            .HasForeignKey(li => li.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Store>()
            .WithMany()
            .HasForeignKey(li => li.StoreId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
