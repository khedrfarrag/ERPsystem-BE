using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RetailOS.Domain.Entities;

namespace RetailOS.Infrastructure.Persistence.Configurations;

public class CashRegisterTransactionConfiguration : IEntityTypeConfiguration<CashRegisterTransaction>
{
    public void Configure(EntityTypeBuilder<CashRegisterTransaction> builder)
    {
        builder.ToTable("cash_register_transactions");

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
        builder.HasIndex(t => new { t.StoreId, t.CreatedAt });

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
