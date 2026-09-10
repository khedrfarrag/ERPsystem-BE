using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RetailOS.Domain.Entities;

namespace RetailOS.Infrastructure.Persistence.Configurations;

public class MerchantConfiguration : IEntityTypeConfiguration<Merchant>
{
    public void Configure(EntityTypeBuilder<Merchant> builder)
    {
        builder.ToTable("merchants");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.TradeName)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(m => m.ContactPerson)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(m => m.Phone)
            .IsRequired()
            .HasMaxLength(30);

        builder.Property(m => m.Email)
            .HasMaxLength(150);

        builder.Property(m => m.Address)
            .HasMaxLength(250);

        builder.Property(m => m.CreditLimit)
            .IsRequired()
            .HasPrecision(19, 4)
            .HasDefaultValue(0m);

        builder.Property(m => m.PaymentTerms)
            .HasMaxLength(50);

        builder.Property(m => m.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.HasIndex(m => m.StoreId);
        builder.HasIndex(m => m.CustomerId);
        builder.HasIndex(m => m.UserId);
        builder.HasIndex(m => new { m.StoreId, m.Phone });

        builder.HasOne<Store>()
            .WithMany()
            .HasForeignKey(m => m.StoreId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(m => m.Customer)
            .WithMany()
            .HasForeignKey(m => m.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(m => m.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
