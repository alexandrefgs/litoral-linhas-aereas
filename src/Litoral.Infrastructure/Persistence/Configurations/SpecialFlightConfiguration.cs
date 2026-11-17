using Litoral.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Litoral.Infrastructure.Persistence.Configurations;

public sealed class SpecialFlightConfiguration : IEntityTypeConfiguration<SpecialFlight>
{
    public void Configure(EntityTypeBuilder<SpecialFlight> builder)
    {
        builder.HasKey(f => f.Id);
        builder.Property(f => f.Title).HasMaxLength(SpecialFlight.MaxTitleLength).IsRequired();
        builder.Property(f => f.Description).HasMaxLength(SpecialFlight.MaxDescriptionLength);
        builder.Property(f => f.Type).HasConversion<string>().HasMaxLength(20);
        builder.Property(f => f.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(f => f.PhotoUrl).HasMaxLength(300);
        builder.Property(f => f.PhotoCredit).HasMaxLength(150);
        builder.HasIndex(f => new { f.Status, f.EndsAt });

        builder.HasOne(f => f.Aircraft).WithMany().HasForeignKey(f => f.AircraftId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(f => f.Pilot).WithMany().HasForeignKey(f => f.PilotId).OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(f => f.Legs).WithOne(l => l.SpecialFlight).HasForeignKey(l => l.SpecialFlightId).OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(f => f.Legs).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Ignore(f => f.OrderedLegs);
        builder.Ignore(f => f.NextLeg);
        builder.Ignore(f => f.IsLocked);
    }
}

public sealed class SpecialFlightLegConfiguration : IEntityTypeConfiguration<SpecialFlightLeg>
{
    public void Configure(EntityTypeBuilder<SpecialFlightLeg> builder)
    {
        builder.HasKey(l => l.Id);
        builder.HasIndex(l => new { l.SpecialFlightId, l.Order }).IsUnique();
        builder.HasIndex(l => l.BookingId);

        builder.HasOne(l => l.Route).WithMany().HasForeignKey(l => l.RouteId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(l => l.Booking).WithMany().HasForeignKey(l => l.BookingId).OnDelete(DeleteBehavior.Restrict);
    }
}