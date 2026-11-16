using Litoral.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Litoral.Infrastructure.Persistence.Configurations;

public sealed class AirportConfiguration : IEntityTypeConfiguration<Airport>
{
    public void Configure(EntityTypeBuilder<Airport> builder)
    {
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Icao).HasMaxLength(4).IsRequired();
        builder.HasIndex(a => a.Icao).IsUnique();
        builder.Property(a => a.Name).HasMaxLength(120).IsRequired();
        builder.Property(a => a.City).HasMaxLength(80).IsRequired();
        builder.Property(a => a.Country).HasMaxLength(60).IsRequired();
        builder.Property(a => a.MinimumRankLevel).HasDefaultValue(1);
    }
}