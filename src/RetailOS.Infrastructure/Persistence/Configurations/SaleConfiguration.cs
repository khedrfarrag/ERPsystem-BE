using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RetailOS.Domain.Entities;

namespace RetailOS.Infrastructure.Persistence.Configurations;

public class SaleConfiguration : IEntityTypeConfiguration<Sale>
{
    public void Configure(EntityTypeBuilder<Sale> builder)
    {
        builder.ToTable("sales");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.InvoiceNumber)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(s => s.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(s => s.PaymentMethod)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(s => s.SubTotal)
            .IsRequired()
            .HasPrecision(19, 4);

        builder.Property(s => s.DiscountAmount)
            .IsRequired()
            .HasPrecision(19, 4);

        builder.Property(s => s.TaxAmount)
            .IsRequired()
            .HasPrecision(19, 4);

        builder.Property(s => s.TotalAmount)
            .IsRequired()
            .HasPrecision(19, 4);

        builder.Property(s => s.CashAmount)
            .IsRequired()
            .HasPrecision(19, 4);

        builder.Property(s => s.CreditAmount)
            .IsRequired()
            .HasPrecision(19, 4);

        builder.Property(s => s.TotalCost)
            .IsRequired()
            .HasPrecision(19, 4);

        builder.Property(s => s.EtaUuid)
            .HasMaxLength(100);

        builder.Property(s => s.SubmissionStatus)
            .HasMaxLength(50);

        builder.Property(s => s.Notes)
            .HasMaxLength(1000);

        builder.HasIndex(s => s.StoreId);
        builder.HasIndex(s => s.CustomerId);
        builder.HasIndex(s => new { s.StoreId, s.InvoiceNumber }).IsUnique();

        builder.HasOne(s => s.Customer)
            .WithMany(c => c.Sales)
            .HasForeignKey(s => s.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(s => s.User)
            .WithMany()
            .HasForeignKey(s => s.CreatedBy)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Store>()
            .WithMany()
            .HasForeignKey(s => s.StoreId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
