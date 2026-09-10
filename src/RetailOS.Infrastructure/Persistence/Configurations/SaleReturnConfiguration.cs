using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RetailOS.Domain.Entities;

namespace RetailOS.Infrastructure.Persistence.Configurations;

public class SaleReturnConfiguration : IEntityTypeConfiguration<SaleReturn>
{
    public void Configure(EntityTypeBuilder<SaleReturn> builder)
    {
        builder.ToTable("sale_returns");

        builder.HasKey(sr => sr.Id);

        builder.Property(sr => sr.ReturnNumber)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(sr => sr.TotalAmount)
            .IsRequired()
            .HasPrecision(19, 4);

        builder.Property(sr => sr.TotalCost)
            .IsRequired()
            .HasPrecision(19, 4);

        builder.Property(sr => sr.RefundMethod)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(sr => sr.Reason)
            .IsRequired()
            .HasMaxLength(1000);

        builder.HasIndex(sr => sr.StoreId);
        builder.HasIndex(sr => sr.SaleId);
        builder.HasIndex(sr => new { sr.StoreId, sr.ReturnNumber }).IsUnique();

        builder.HasOne(sr => sr.Sale)
            .WithMany(s => s.Returns)
            .HasForeignKey(sr => sr.SaleId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(sr => sr.User)
            .WithMany()
            .HasForeignKey(sr => sr.CreatedBy)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Store>()
            .WithMany()
            .HasForeignKey(sr => sr.StoreId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
