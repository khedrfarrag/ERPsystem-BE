using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RetailOS.Domain.Entities;

namespace RetailOS.Infrastructure.Persistence.Configurations;

public class IdempotencyRecordConfiguration : IEntityTypeConfiguration<IdempotencyRecord>
{
    public void Configure(EntityTypeBuilder<IdempotencyRecord> builder)
    {
        builder.ToTable("idempotency_records");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Key)
            .IsRequired()
            .HasMaxLength(128);

        builder.Property(r => r.Endpoint)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(r => r.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.HasIndex(r => r.StoreId);
        builder.HasIndex(r => new { r.StoreId, r.Key }).IsUnique();

        builder.HasOne<Store>()
            .WithMany()
            .HasForeignKey(r => r.StoreId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
