using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RetailOS.Domain.Entities;

namespace RetailOS.Infrastructure.Persistence.Configurations;

public class CustomerAccountTransactionConfiguration : IEntityTypeConfiguration<CustomerAccountTransaction>
{
    public void Configure(EntityTypeBuilder<CustomerAccountTransaction> builder)
    {
        builder.ToTable("customer_account_transactions");

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
        builder.HasIndex(t => t.CustomerId);
        builder.HasIndex(t => new { t.StoreId, t.CustomerId, t.CreatedAt });

        builder.HasOne(t => t.Customer)
            .WithMany(c => c.AccountTransactions)
            .HasForeignKey(t => t.CustomerId)
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
