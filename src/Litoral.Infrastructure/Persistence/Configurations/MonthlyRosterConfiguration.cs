using Litoral.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Litoral.Infrastructure.Persistence.Configurations;

public sealed class MonthlyRosterConfiguration : IEntityTypeConfiguration<MonthlyRoster>
{
    public void Configure(EntityTypeBuilder<MonthlyRoster> builder)
    {
        builder.HasKey(r => r.Id);
        builder.Property(r => r.BaseIcao).HasMaxLength(4).IsRequired();
        builder.HasIndex(r => new { r.PilotId, r.Year, r.Month }).IsUnique();

        builder.Ignore(r => r.NextLeg);
        builder.Ignore(r => r.Bonus);

        builder.HasOne(r => r.Pilot).WithMany().HasForeignKey(r => r.PilotId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(r => r.Legs).WithOne(l => l.Roster).HasForeignKey(l => l.RosterId).OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(r => r.Legs).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

public sealed class RosterLegConfiguration : IEntityTypeConfiguration<RosterLeg>
{
    public void Configure(EntityTypeBuilder<RosterLeg> builder)
    {
        builder.HasKey(l => l.Id);
        builder.HasIndex(l => new { l.RosterId, l.Order }).IsUnique();
        builder.HasIndex(l => l.BookingId);

        builder.HasOne(l => l.Route).WithMany().HasForeignKey(l => l.RouteId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(l => l.Booking).WithMany().HasForeignKey(l => l.BookingId).OnDelete(DeleteBehavior.Restrict);
    }
}