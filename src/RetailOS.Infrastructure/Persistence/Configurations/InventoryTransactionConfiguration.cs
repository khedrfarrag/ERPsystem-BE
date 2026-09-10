using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RetailOS.Domain.Entities;

namespace RetailOS.Infrastructure.Persistence.Configurations;

public class InventoryTransactionConfiguration : IEntityTypeConfiguration<InventoryTransaction>
{
    public void Configure(EntityTypeBuilder<InventoryTransaction> builder)
    {
        builder.ToTable("inventory_transactions");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Quantity)
            .IsRequired()
            .HasPrecision(19, 4);

        builder.Property(t => t.CostPerUnit)
            .IsRequired()
            .HasPrecision(19, 6);

        builder.Property(t => t.Reason)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(30);

        builder.Property(t => t.Notes)
            .HasMaxLength(1000);

        builder.HasIndex(t => new { t.StoreId, t.ProductId });
        builder.HasIndex(t => new { t.StoreId, t.CreatedAt });

        builder.HasOne(t => t.Product)
            .WithMany(p => p.InventoryTransactions)
            .HasForeignKey(t => t.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(t => t.User)
            .WithMany()
            .HasForeignKey(t => t.CreatedBy)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Store>()
            .WithMany()
            .HasForeignKey(t => t.StoreId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
