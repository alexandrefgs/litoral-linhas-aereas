using Litoral.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Litoral.Infrastructure.Persistence.Configurations;

public sealed class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.HasKey(l => l.Id);

        builder.Property(l => l.Category).HasConversion<string>().HasMaxLength(20);
        builder.Property(l => l.Action).HasMaxLength(AuditLog.MaxActionLength).IsRequired();
        builder.Property(l => l.ActorUserId).HasMaxLength(450);
        builder.Property(l => l.ActorName).HasMaxLength(AuditLog.MaxNameLength).IsRequired();
        builder.Property(l => l.TargetType).HasMaxLength(40);
        builder.Property(l => l.TargetId).HasMaxLength(100);
        builder.Property(l => l.TargetName).HasMaxLength(AuditLog.MaxNameLength);
        builder.Property(l => l.Details).HasMaxLength(AuditLog.MaxDetailsLength);
        builder.Property(l => l.IpAddress).HasMaxLength(45);

        builder.HasIndex(l => l.At);
        builder.HasIndex(l => l.Category);
    }
}