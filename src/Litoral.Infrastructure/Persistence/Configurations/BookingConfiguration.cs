using Litoral.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Litoral.Infrastructure.Persistence.Configurations;

public sealed class BookingConfiguration : IEntityTypeConfiguration<Booking>
{
    public void Configure(EntityTypeBuilder<Booking> builder)
    {
        builder.HasKey(b => b.Id);
        builder.Property(b => b.Callsign).HasMaxLength(7).IsRequired();
        builder.Property(b => b.Status).HasConversion<string>().HasMaxLength(20);
        builder.HasIndex(b => new { b.PilotId, b.Status });

        builder.Ignore(b => b.IsOpen);

        builder.HasOne(b => b.Pilot).WithMany().HasForeignKey(b => b.PilotId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(b => b.Route).WithMany().HasForeignKey(b => b.RouteId).OnDelete(DeleteBehavior.Restrict);
    }
}