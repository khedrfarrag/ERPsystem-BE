using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RetailOS.Domain.Entities;

namespace RetailOS.Infrastructure.Persistence.Configurations;

public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("notifications");

        builder.HasKey(n => n.Id);

        builder.Property(n => n.Title)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(n => n.Message)
            .IsRequired()
            .HasMaxLength(300);

        builder.Property(n => n.NotificationType)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(n => n.ReferenceId)
            .HasMaxLength(100);

        builder.Property(n => n.IsRead)
            .IsRequired()
            .HasDefaultValue(false);

        builder.HasIndex(n => n.StoreId);
        builder.HasIndex(n => n.RecipientUserId);
        builder.HasIndex(n => new { n.StoreId, n.IsRead });

        builder.HasOne<Store>()
            .WithMany()
            .HasForeignKey(n => n.StoreId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(n => n.RecipientUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
