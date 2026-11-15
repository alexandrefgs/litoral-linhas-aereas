using Litoral.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Litoral.Infrastructure.Persistence.Configurations;

public sealed class AircraftTypeConfiguration : IEntityTypeConfiguration<AircraftType>
{
    public void Configure(EntityTypeBuilder<AircraftType> builder)
    {
        builder.HasKey(a => a.Id);
        builder.Property(a => a.IcaoCode).HasMaxLength(4).IsRequired();
        builder.Property(a => a.Name).HasMaxLength(60).IsRequired();
        builder.HasIndex(a => new { a.IcaoCode, a.IsCargo }).IsUnique();
    }
}