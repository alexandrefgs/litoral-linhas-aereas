using Litoral.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Litoral.Infrastructure.Persistence.Configurations;

public sealed class PrivacyRequestConfiguration : IEntityTypeConfiguration<PrivacyRequest>
{
    public void Configure(EntityTypeBuilder<PrivacyRequest> builder)
    {
        builder.HasKey(r => r.Id);

        builder.Property(r => r.PilotLabel).HasMaxLength(PrivacyRequest.MaxNameLength).IsRequired();
        builder.Property(r => r.Type).HasConversion<string>().HasMaxLength(30);
        builder.Property(r => r.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(r => r.Message).HasMaxLength(PrivacyRequest.MaxMessageLength).IsRequired();
        builder.Property(r => r.Response).HasMaxLength(PrivacyRequest.MaxResponseLength);
        builder.Property(r => r.ResolvedBy).HasMaxLength(PrivacyRequest.MaxNameLength);
        builder.Ignore(r => r.IsOpen);

        builder.HasOne(r => r.Pilot)
            .WithMany()
            .HasForeignKey(r => r.PilotId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(r => new { r.Status, r.DueAt });
        builder.HasIndex(r => r.PilotId);
    }
}