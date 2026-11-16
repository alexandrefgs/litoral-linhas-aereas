using Litoral.Domain.Entities;
using Litoral.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Litoral.Infrastructure.Persistence.Configurations;

public sealed class PilotConfiguration : IEntityTypeConfiguration<Pilot>
{
    public void Configure(EntityTypeBuilder<Pilot> builder)
    {
        builder.HasKey(p => p.Id);
        builder.HasIndex(p => p.PilotNumber).IsUnique();
        builder.HasIndex(p => p.UserId).IsUnique();
        builder.Property(p => p.FirstName).HasMaxLength(60).IsRequired();
        builder.Property(p => p.LastName).HasMaxLength(60).IsRequired();
        builder.Property(p => p.CurrentLocationIcao).HasMaxLength(4).IsRequired();
        builder.Property(p => p.SimBriefUsername).HasMaxLength(60);

        builder.Ignore(p => p.DisplayId);
        builder.Ignore(p => p.NetworkCallsign);

        builder.HasOne(p => p.Rank).WithMany().HasForeignKey(p => p.RankId).OnDelete(DeleteBehavior.Restrict);
        builder.Navigation(p => p.PointTransactions).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.HasOne<ApplicationUser>().WithOne().HasForeignKey<Pilot>(p => p.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}