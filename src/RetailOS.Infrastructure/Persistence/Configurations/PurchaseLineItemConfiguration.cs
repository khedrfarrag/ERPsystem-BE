using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RetailOS.Domain.Entities;

namespace RetailOS.Infrastructure.Persistence.Configurations;

public class PurchaseLineItemConfiguration : IEntityTypeConfiguration<PurchaseLineItem>
{
    public void Configure(EntityTypeBuilder<PurchaseLineItem> builder)
    {
        builder.ToTable("purchase_line_items");

        builder.HasKey(li => li.Id);

        builder.Property(li => li.Quantity)
            .IsRequired()
            .HasPrecision(19, 4);

        builder.Property(li => li.UnitCost)
            .IsRequired()
            .HasPrecision(19, 6);

        builder.Property(li => li.Discount)
            .IsRequired()
            .HasPrecision(19, 4);

        builder.Property(li => li.SubTotal)
            .IsRequired()
            .HasPrecision(19, 4);

        builder.HasIndex(li => li.StoreId);
        builder.HasIndex(li => li.PurchaseId);
        builder.HasIndex(li => li.ProductId);

        builder.HasOne(li => li.Purchase)
            .WithMany(p => p.LineItems)
            .HasForeignKey(li => li.PurchaseId)
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
