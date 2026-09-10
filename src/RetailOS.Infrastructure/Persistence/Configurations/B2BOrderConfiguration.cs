using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RetailOS.Domain.Entities;

namespace RetailOS.Infrastructure.Persistence.Configurations;

public class B2BOrderConfiguration : IEntityTypeConfiguration<B2BOrder>
{
    public void Configure(EntityTypeBuilder<B2BOrder> builder)
    {
        builder.ToTable("b2b_orders");

        builder.HasKey(o => o.Id);

        builder.Property(o => o.OrderNumber)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(o => o.Status)
            .IsRequired()
            .HasMaxLength(30)
            .HasDefaultValue("Pending");

        builder.Property(o => o.PaymentPreference)
            .IsRequired()
            .HasMaxLength(30)
            .HasDefaultValue("Credit");

        builder.Property(o => o.TotalAmount)
            .IsRequired()
            .HasPrecision(19, 4);

        builder.Property(o => o.PaidAmount)
            .IsRequired()
            .HasPrecision(19, 4)
            .HasDefaultValue(0m);

        builder.Property(o => o.RemainingAmount)
            .IsRequired()
            .HasPrecision(19, 4)
            .HasDefaultValue(0m);

        builder.Property(o => o.Notes)
            .HasMaxLength(500);

        builder.Property(o => o.RejectionReason)
            .HasMaxLength(300);

        builder.Property(o => o.CancellationReason)
            .HasMaxLength(300);

        builder.Property(o => o.IsCreditLimitOverrideUsed)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(o => o.CreditLimitOverrideReason)
            .HasMaxLength(500);

        builder.Property(o => o.CreditLimitAtInvoice)
            .HasPrecision(19, 4);

        builder.Property(o => o.OutstandingBalanceAtInvoice)
            .HasPrecision(19, 4);

        builder.Property(o => o.CreditAmountAtInvoice)
            .HasPrecision(19, 4);

        builder.Property(o => o.ProjectedBalanceAtInvoice)
            .HasPrecision(19, 4);

        builder.Property(o => o.CreditLimitExceededBy)
            .HasPrecision(19, 4);

        builder.HasIndex(o => o.StoreId);
        builder.HasIndex(o => o.MerchantId);
        builder.HasIndex(o => new { o.StoreId, o.OrderNumber }).IsUnique();
        builder.HasIndex(o => new { o.StoreId, o.Status });
        builder.HasIndex(o => o.SalesInvoiceId);

        builder.HasOne<Store>()
            .WithMany()
            .HasForeignKey(o => o.StoreId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(o => o.Merchant)
            .WithMany(m => m.Orders)
            .HasForeignKey(o => o.MerchantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(o => o.SalesInvoice)
            .WithMany()
            .HasForeignKey(o => o.SalesInvoiceId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(o => o.CancelledByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(o => o.CreditLimitOverrideByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
