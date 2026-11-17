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
        builder.Property(a => a.MaintenanceIntervalHours).HasDefaultValue(AircraftType.DefaultMaintenanceIntervalHours);
        builder.Property(a => a.CCheckIntervalHours).HasDefaultValue(AircraftType.DefaultCCheckIntervalHours);
        builder.Property(a => a.DCheckIntervalHours).HasDefaultValue(AircraftType.DefaultDCheckIntervalHours);
        builder.Property(a => a.RegistrationPrefix).HasMaxLength(2);
        builder.Property(a => a.DeliveryAirportIcao).HasMaxLength(4);
        builder.HasIndex(a => new { a.IcaoCode, a.IsCargo }).IsUnique();
        builder.HasIndex(a => a.RegistrationPrefix).IsUnique();

        builder.Ignore(a => a.Operation);
        builder.Ignore(a => a.RegistrationStart);
    }
}