using Litoral.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Litoral.Infrastructure.Persistence.Configurations;

public sealed class AircraftConfiguration : IEntityTypeConfiguration<Aircraft>
{
    public void Configure(EntityTypeBuilder<Aircraft> builder)
    {
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Registration).HasMaxLength(6).IsRequired();
        builder.HasIndex(a => a.Registration).IsUnique();
        builder.Property(a => a.Variant).HasMaxLength(40);
        builder.Property(a => a.PhotoUrl).HasMaxLength(500);
        builder.Property(a => a.PhotoCredit).HasMaxLength(120);
        builder.Property(a => a.MaintenanceReason).HasMaxLength(300);
        builder.Property(a => a.Status).HasConversion<string>().HasMaxLength(20);
        builder.HasIndex(a => new { a.CurrentAirportId, a.Status });

        builder.Ignore(a => a.IsAvailable);
        builder.Ignore(a => a.IsInUse);
        builder.Ignore(a => a.IsMaintenanceDue);

        builder.HasOne(a => a.AircraftType).WithMany().HasForeignKey(a => a.AircraftTypeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(a => a.CurrentAirport).WithMany().HasForeignKey(a => a.CurrentAirportId).OnDelete(DeleteBehavior.Restrict);
    }
}