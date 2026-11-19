using Litoral.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Litoral.Infrastructure.Persistence.Configurations;

public sealed class AcarsKeyConfiguration : IEntityTypeConfiguration<AcarsKey>
{
    public void Configure(EntityTypeBuilder<AcarsKey> builder)
    {
        builder.HasKey(k => k.Id);
        builder.Property(k => k.Name).HasMaxLength(AcarsKey.MaxNameLength).IsRequired();
        builder.Property(k => k.Prefix).HasMaxLength(AcarsKey.PrefixLength).IsRequired();
        builder.Property(k => k.Hash).HasMaxLength(64).IsRequired();
        builder.Ignore(k => k.IsActive);
        builder.HasIndex(k => k.Hash).IsUnique();
        builder.HasIndex(k => new { k.PilotId, k.RevokedAt });

        builder.HasOne(k => k.Pilot).WithMany().HasForeignKey(k => k.PilotId).OnDelete(DeleteBehavior.Cascade);
    }
}