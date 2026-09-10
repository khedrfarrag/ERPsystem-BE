using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RetailOS.Domain.Entities;

namespace RetailOS.Infrastructure.Persistence.Configurations;

public class B2BOrderItemConfiguration : IEntityTypeConfiguration<B2BOrderItem>
{
    public void Configure(EntityTypeBuilder<B2BOrderItem> builder)
    {
        builder.ToTable("b2b_order_items");

        builder.HasKey(i => i.Id);

        builder.Property(i => i.ProductName)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(i => i.RequestedQuantity)
            .IsRequired()
            .HasPrecision(19, 4);

        builder.Property(i => i.ApprovedQuantity)
            .HasPrecision(19, 4);

        builder.Property(i => i.UnitWholesalePrice)
            .IsRequired()
            .HasPrecision(19, 4);

        builder.Property(i => i.RequestedSubtotal)
            .IsRequired()
            .HasPrecision(19, 4);

        builder.Property(i => i.ApprovedSubtotal)
            .HasPrecision(19, 4);

        builder.Property(i => i.AdjustmentReason)
            .HasMaxLength(300);

        builder.HasIndex(i => i.StoreId);
        builder.HasIndex(i => i.B2BOrderId);
        builder.HasIndex(i => i.ProductId);

        builder.HasOne<Store>()
            .WithMany()
            .HasForeignKey(i => i.StoreId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(i => i.B2BOrder)
            .WithMany(o => o.Items)
            .HasForeignKey(i => i.B2BOrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(i => i.Product)
            .WithMany()
            .HasForeignKey(i => i.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(i => i.AdjustedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
