using Litoral.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Litoral.Infrastructure.Persistence.Configurations;

public sealed class PirepConfiguration : IEntityTypeConfiguration<Pirep>
{
    public void Configure(EntityTypeBuilder<Pirep> builder)
    {
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Source).HasConversion<string>().HasMaxLength(20);
        builder.Property(p => p.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(p => p.Network).HasConversion<string>().HasMaxLength(20);
        builder.Property(p => p.Callsign).HasMaxLength(10).IsRequired();
        builder.Property(p => p.DepartureIcao).HasMaxLength(4).IsRequired();
        builder.Property(p => p.PlannedArrivalIcao).HasMaxLength(4).IsRequired();
        builder.Property(p => p.ArrivalIcao).HasMaxLength(10).IsRequired();
        builder.Property(p => p.Registration).HasMaxLength(10).IsRequired();
        builder.Property(p => p.AircraftTypeIcao).HasMaxLength(4).IsRequired();
        builder.Property(p => p.FlightRoute).HasMaxLength(1000);
        builder.Property(p => p.Comments).HasMaxLength(Pirep.MaxCommentsLength);
        builder.Property(p => p.ReviewedBy).HasMaxLength(450);
        builder.Property(p => p.ReviewNote).HasMaxLength(1000);
        builder.Property(p => p.FuelUsedKg).HasPrecision(9, 1);
        builder.Property(p => p.FuelAtPushbackKg).HasPrecision(9, 1);
        builder.Property(p => p.OnlinePercent).HasPrecision(5, 2);

        builder.Ignore(p => p.IsDiversion);

        builder.HasOne(p => p.Booking).WithMany().HasForeignKey(p => p.BookingId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(p => p.Pilot).WithMany().HasForeignKey(p => p.PilotId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(p => p.FlightSession).WithMany().HasForeignKey(p => p.FlightSessionId).OnDelete(DeleteBehavior.Restrict);

        builder.OwnsMany(p => p.Items, item =>
        {
            item.ToTable("PirepScoreItems");
            item.WithOwner().HasForeignKey("PirepId");
            item.Property<int>("Id");
            item.HasKey("Id");
            item.Property(i => i.Category).HasConversion<string>().HasMaxLength(40);
            item.Property(i => i.Check).HasConversion<string>().HasMaxLength(40);
            item.Property(i => i.Outcome).HasConversion<string>().HasMaxLength(20);
            item.Property(i => i.Description).HasMaxLength(ScoringRule.MaxDescriptionLength).IsRequired();
        });
        builder.Navigation(p => p.Items).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.OwnsMany(p => p.Events, e =>
        {
            e.ToTable("PirepEvents");
            e.WithOwner().HasForeignKey("PirepId");
            e.Property<int>("Id");
            e.HasKey("Id");
            e.Property(x => x.Type).HasConversion<string>().HasMaxLength(20);
            e.Property(x => x.Detail).HasMaxLength(200);
            e.Property(x => x.Value).HasPrecision(9, 2);
        });
        builder.Navigation(p => p.Events).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(p => p.BookingId).IsUnique();
        builder.HasIndex(p => new { p.PilotId, p.SubmittedAt });
        builder.HasIndex(p => new { p.Status, p.SubmittedAt });
    }
}