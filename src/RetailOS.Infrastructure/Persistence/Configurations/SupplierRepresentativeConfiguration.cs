using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RetailOS.Domain.Entities;

namespace RetailOS.Infrastructure.Persistence.Configurations;

public class SupplierRepresentativeConfiguration : IEntityTypeConfiguration<SupplierRepresentative>
{
    public void Configure(EntityTypeBuilder<SupplierRepresentative> builder)
    {
        builder.ToTable("supplier_representatives");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(r => r.Phone)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(r => r.Notes)
            .HasMaxLength(1000);

        builder.Property(r => r.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.HasIndex(r => r.StoreId);
        builder.HasIndex(r => r.SupplierId);

        builder.HasOne(r => r.Supplier)
            .WithMany(s => s.Representatives)
            .HasForeignKey(r => r.SupplierId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Store>()
            .WithMany()
            .HasForeignKey(r => r.StoreId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
