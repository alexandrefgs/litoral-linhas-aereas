using Litoral.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Litoral.Infrastructure.Persistence.Configurations;

public sealed class RouteConfiguration : IEntityTypeConfiguration<Route>
{
    public void Configure(EntityTypeBuilder<Route> builder)
    {
        builder.HasKey(r => r.Id);
        builder.HasIndex(r => r.FlightNumber).IsUnique();

        builder.Ignore(r => r.Block);
        builder.Ignore(r => r.Callsign);

        builder.HasOne(r => r.DepartureAirport).WithMany().HasForeignKey(r => r.DepartureAirportId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(r => r.ArrivalAirport).WithMany().HasForeignKey(r => r.ArrivalAirportId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(r => r.AircraftType).WithMany().HasForeignKey(r => r.AircraftTypeId).OnDelete(DeleteBehavior.Restrict);
    }
}