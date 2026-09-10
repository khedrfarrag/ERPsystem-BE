using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RetailOS.Domain.Entities;

namespace RetailOS.Infrastructure.Persistence.Configurations;

public class StoreConfiguration : IEntityTypeConfiguration<Store>
{
    public void Configure(EntityTypeBuilder<Store> builder)
    {
        builder.HasKey(s => s.Id);

        builder.Property(s => s.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(s => s.BusinessType)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(s => s.Phone)
            .HasMaxLength(50);

        builder.Property(s => s.Currency)
            .IsRequired()
            .HasMaxLength(10)
            .HasDefaultValue("EGP");

        builder.Property(s => s.Timezone)
            .IsRequired()
            .HasMaxLength(100)
            .HasDefaultValue("Africa/Cairo");

        builder.Property(s => s.InvoicePrefix)
            .HasMaxLength(20)
            .HasDefaultValue("INV");

        builder.Property(s => s.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(s => s.CreatedAt)
            .IsRequired();

        builder.Property(s => s.UpdatedAt)
            .IsRequired();

        builder.HasMany(s => s.Users)
            .WithOne(u => u.Store)
            .HasForeignKey(u => u.StoreId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(s => s.RefreshTokens)
            .WithOne(rt => rt.Store)
            .HasForeignKey(rt => rt.StoreId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
