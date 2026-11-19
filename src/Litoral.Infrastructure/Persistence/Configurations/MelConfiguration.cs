using Litoral.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Litoral.Infrastructure.Persistence.Configurations;

public sealed class MelItemConfiguration : IEntityTypeConfiguration<MelItem>
{
    public void Configure(EntityTypeBuilder<MelItem> builder)
    {
        builder.HasKey(m => m.Id);
        builder.Property(m => m.Code).HasMaxLength(MelItem.MaxCodeLength).IsRequired();
        builder.Property(m => m.Title).HasMaxLength(MelItem.MaxTitleLength).IsRequired();
        builder.Property(m => m.Restriction).HasMaxLength(MelItem.MaxTextLength).IsRequired();
        builder.Property(m => m.Procedure).HasMaxLength(MelItem.MaxTextLength).IsRequired();
        builder.Property(m => m.CheckText).HasMaxLength(MelItem.MaxCheckTextLength);
        builder.Property(m => m.AircraftTypes).HasMaxLength(MelItem.MaxAircraftTypesLength).IsRequired();
        builder.Property(m => m.FuelPenaltyPercent).HasPrecision(4, 1);
        builder.Property(m => m.Kind).HasConversion<string>().HasMaxLength(10);
        builder.Property(m => m.Category).HasConversion<string>().HasMaxLength(2);
        builder.Property(m => m.Verification).HasConversion<string>().HasMaxLength(10);
        builder.Property(m => m.AcarsCheck).HasConversion<string>().HasMaxLength(30);
        builder.Ignore(m => m.AircraftTypeList);
        builder.Ignore(m => m.Days);
        builder.HasIndex(m => m.Code).IsUnique();
    }
}

public sealed class AircraftMelItemConfiguration : IEntityTypeConfiguration<AircraftMelItem>
{
    public void Configure(EntityTypeBuilder<AircraftMelItem> builder)
    {
        builder.HasKey(m => m.Id);
        builder.Property(m => m.Source).HasConversion<string>().HasMaxLength(10);
        builder.Property(m => m.OpenedBy).HasMaxLength(150).IsRequired();
        builder.Property(m => m.ClosedBy).HasMaxLength(150);
        builder.Property(m => m.CloseReason).HasMaxLength(100);
        builder.Property(m => m.Note).HasMaxLength(AircraftMelItem.MaxNoteLength);
        builder.Ignore(m => m.IsOpen);
        builder.HasIndex(m => new { m.AircraftId, m.ClosedAt });

        builder.HasOne(m => m.Aircraft).WithMany().HasForeignKey(m => m.AircraftId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(m => m.MelItem).WithMany().HasForeignKey(m => m.MelItemId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class FlightMelCheckConfiguration : IEntityTypeConfiguration<FlightMelCheck>
{
    public void Configure(EntityTypeBuilder<FlightMelCheck> builder)
    {
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Result).HasConversion<string>().HasMaxLength(10);
        builder.Property(c => c.Detail).HasMaxLength(200);
        builder.Property(c => c.DecidedBy).HasMaxLength(150);
        builder.HasIndex(c => new { c.PirepId, c.AircraftMelItemId }).IsUnique();

        builder.HasOne<Pirep>().WithMany().HasForeignKey(c => c.PirepId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(c => c.AircraftMelItem).WithMany().HasForeignKey(c => c.AircraftMelItemId).OnDelete(DeleteBehavior.Restrict);
    }
}