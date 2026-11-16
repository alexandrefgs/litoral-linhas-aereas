using Litoral.Infrastructure.Reference;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Litoral.Infrastructure.Persistence.Configurations;

public sealed class AirportCatalogEntryConfiguration : IEntityTypeConfiguration<AirportCatalogEntry>
{
    public void Configure(EntityTypeBuilder<AirportCatalogEntry> builder)
    {
        builder.ToTable("AirportCatalog");
        builder.HasKey(a => a.Icao);
        builder.Property(a => a.Icao).HasMaxLength(4);
        builder.Property(a => a.IataCode).HasMaxLength(3);
        builder.Property(a => a.Name).HasMaxLength(150).IsRequired();
        builder.Property(a => a.City).HasMaxLength(100).IsRequired();
        builder.Property(a => a.CountryCode).HasMaxLength(2).IsRequired();
        builder.Property(a => a.CountryName).HasMaxLength(100).IsRequired();
        builder.Property(a => a.Continent).HasMaxLength(2).IsRequired();
        builder.Property(a => a.Type).HasMaxLength(20).IsRequired();
    }
}