using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RetailOS.Domain.Entities;

namespace RetailOS.Infrastructure.Persistence.Configurations;

public class PurchaseConfiguration : IEntityTypeConfiguration<Purchase>
{
    public void Configure(EntityTypeBuilder<Purchase> builder)
    {
        builder.ToTable("purchases");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.PurchaseNumber)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(p => p.InvoiceNumber)
            .HasMaxLength(100);

        builder.Property(p => p.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(p => p.TotalAmount)
            .IsRequired()
            .HasPrecision(19, 4);

        builder.Property(p => p.Notes)
            .HasMaxLength(1000);

        builder.HasIndex(p => p.StoreId);
        builder.HasIndex(p => p.SupplierId);
        builder.HasIndex(p => new { p.StoreId, p.PurchaseNumber }).IsUnique();

        builder.HasOne(p => p.Supplier)
            .WithMany(s => s.Purchases)
            .HasForeignKey(p => p.SupplierId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.User)
            .WithMany()
            .HasForeignKey(p => p.CreatedBy)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Store>()
            .WithMany()
            .HasForeignKey(p => p.StoreId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
