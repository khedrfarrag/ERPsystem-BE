using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RetailOS.Domain.Entities;

namespace RetailOS.Infrastructure.Persistence.Configurations;

public class PurchaseReturnConfiguration : IEntityTypeConfiguration<PurchaseReturn>
{
    public void Configure(EntityTypeBuilder<PurchaseReturn> builder)
    {
        builder.ToTable("purchase_returns");

        builder.HasKey(pr => pr.Id);

        builder.Property(pr => pr.ReturnNumber)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(pr => pr.TotalAmount)
            .IsRequired()
            .HasPrecision(19, 4);

        builder.Property(pr => pr.Reason)
            .IsRequired()
            .HasMaxLength(1000);

        builder.HasIndex(pr => pr.StoreId);
        builder.HasIndex(pr => pr.PurchaseId);
        builder.HasIndex(pr => new { pr.StoreId, pr.ReturnNumber }).IsUnique();

        builder.HasOne(pr => pr.Purchase)
            .WithMany(p => p.Returns)
            .HasForeignKey(pr => pr.PurchaseId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(pr => pr.User)
            .WithMany()
            .HasForeignKey(pr => pr.CreatedBy)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Store>()
            .WithMany()
            .HasForeignKey(pr => pr.StoreId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
