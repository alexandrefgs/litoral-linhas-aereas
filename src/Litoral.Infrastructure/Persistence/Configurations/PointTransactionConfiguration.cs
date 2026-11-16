using Litoral.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Litoral.Infrastructure.Persistence.Configurations;

public sealed class PointTransactionConfiguration : IEntityTypeConfiguration<PointTransaction>
{
    public void Configure(EntityTypeBuilder<PointTransaction> builder)
    {
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Type).HasConversion<string>().HasMaxLength(30);
        builder.Property(t => t.Description).HasMaxLength(200).IsRequired();
        builder.HasIndex(t => new { t.PilotId, t.CreatedAt });

        builder.HasOne(t => t.Pilot)
            .WithMany(p => p.PointTransactions)
            .HasForeignKey(t => t.PilotId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}