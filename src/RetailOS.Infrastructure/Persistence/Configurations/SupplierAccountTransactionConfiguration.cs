using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RetailOS.Domain.Entities;

namespace RetailOS.Infrastructure.Persistence.Configurations;

public class SupplierAccountTransactionConfiguration : IEntityTypeConfiguration<SupplierAccountTransaction>
{
    public void Configure(EntityTypeBuilder<SupplierAccountTransaction> builder)
    {
        builder.ToTable("supplier_account_transactions");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Type)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(t => t.Amount)
            .IsRequired()
            .HasPrecision(19, 4);

        builder.Property(t => t.Notes)
            .HasMaxLength(1000);

        builder.HasIndex(t => t.StoreId);
        builder.HasIndex(t => t.SupplierId);
        builder.HasIndex(t => new { t.StoreId, t.SupplierId, t.CreatedAt });

        builder.HasOne(t => t.Supplier)
            .WithMany(s => s.AccountTransactions)
            .HasForeignKey(t => t.SupplierId)
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
