using Litoral.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Litoral.Infrastructure.Persistence.Configurations;

public sealed class AirlineEventConfiguration : IEntityTypeConfiguration<AirlineEvent>
{
    public void Configure(EntityTypeBuilder<AirlineEvent> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Title).HasMaxLength(AirlineEvent.MaxTitleLength).IsRequired();
        builder.Property(e => e.Description).HasMaxLength(AirlineEvent.MaxDescriptionLength).IsRequired();
        builder.Property(e => e.ImageUrl).HasMaxLength(300);
        builder.Property(e => e.Airports).HasMaxLength(120).IsRequired();
        builder.Ignore(e => e.AirportList);
        builder.HasIndex(e => new { e.IsPublished, e.StartsAt, e.EndsAt });
    }
}

public sealed class AirlineEventFlightConfiguration : IEntityTypeConfiguration<AirlineEventFlight>
{
    public void Configure(EntityTypeBuilder<AirlineEventFlight> builder)
    {
        builder.HasKey(f => f.Id);
        builder.HasIndex(f => f.PirepId).IsUnique();
        builder.HasIndex(f => new { f.EventId, f.PilotId });

        builder.HasOne(f => f.Event).WithMany().HasForeignKey(f => f.EventId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(f => f.Pilot).WithMany().HasForeignKey(f => f.PilotId).OnDelete(DeleteBehavior.Cascade);
    }
}