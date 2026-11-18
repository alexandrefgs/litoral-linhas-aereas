using Litoral.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Litoral.Infrastructure.Persistence.Configurations;

public sealed class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.HasKey(n => n.Id);

        builder.Property(n => n.Type).HasConversion<string>().HasMaxLength(30);
        builder.Property(n => n.Title).HasMaxLength(Notification.MaxTitleLength).IsRequired();
        builder.Property(n => n.Message).HasMaxLength(Notification.MaxMessageLength).IsRequired();
        builder.Property(n => n.Link).HasMaxLength(Notification.MaxLinkLength);
        builder.Ignore(n => n.IsRead);

        builder.HasOne(n => n.Pilot)
            .WithMany()
            .HasForeignKey(n => n.PilotId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(n => new { n.PilotId, n.CreatedAt });
    }
}