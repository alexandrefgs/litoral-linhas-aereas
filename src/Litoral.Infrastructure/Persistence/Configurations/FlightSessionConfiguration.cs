using Litoral.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Litoral.Infrastructure.Persistence.Configurations;

public sealed class FlightSessionConfiguration : IEntityTypeConfiguration<FlightSession>
{
    public void Configure(EntityTypeBuilder<FlightSession> builder)
    {
        builder.HasKey(s => s.Id);

        builder.Property(s => s.Network).HasConversion<string>().HasMaxLength(20);
        builder.Property(s => s.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(s => s.Simulator).HasMaxLength(60);
        builder.Property(s => s.Client).HasMaxLength(60);
        builder.Property(s => s.Phase).HasMaxLength(30);

        builder.HasOne(s => s.Booking).WithMany().HasForeignKey(s => s.BookingId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Pilot>().WithMany().HasForeignKey(s => s.PilotId).OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(s => s.Points).WithOne().HasForeignKey(p => p.SessionId).OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(s => s.Points).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Ignore(s => s.IsActive);

        builder.HasIndex(s => new { s.PilotId, s.Status });
        builder.HasIndex(s => s.Status);
    }
}

public sealed class FlightTrackPointConfiguration : IEntityTypeConfiguration<FlightTrackPoint>
{
    public void Configure(EntityTypeBuilder<FlightTrackPoint> builder)
    {
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).ValueGeneratedOnAdd();
        builder.Property(p => p.Phase).HasMaxLength(30);

        builder.HasIndex(p => new { p.SessionId, p.At });
    }
}