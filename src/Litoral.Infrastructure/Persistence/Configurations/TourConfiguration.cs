using Litoral.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Litoral.Infrastructure.Persistence.Configurations;

public sealed class TourConfiguration : IEntityTypeConfiguration<Tour>
{
    public void Configure(EntityTypeBuilder<Tour> builder)
    {
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Title).HasMaxLength(Tour.MaxTitleLength).IsRequired();
        builder.Property(t => t.Description).HasMaxLength(Tour.MaxDescriptionLength).IsRequired();
        builder.Property(t => t.ImageUrl).HasMaxLength(300);
        builder.HasIndex(t => new { t.IsPublished, t.EndsAt });

        builder.HasOne(t => t.Award).WithMany().HasForeignKey(t => t.AwardId).OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(t => t.Legs).WithOne(l => l.Tour).HasForeignKey(l => l.TourId).OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(t => t.Legs).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Ignore(t => t.OrderedLegs);
    }
}

public sealed class TourLegConfiguration : IEntityTypeConfiguration<TourLeg>
{
    public void Configure(EntityTypeBuilder<TourLeg> builder)
    {
        builder.HasKey(l => l.Id);
        builder.HasIndex(l => new { l.TourId, l.Order }).IsUnique();
        builder.HasOne(l => l.Route).WithMany().HasForeignKey(l => l.RouteId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class TourProgressConfiguration : IEntityTypeConfiguration<TourProgress>
{
    public void Configure(EntityTypeBuilder<TourProgress> builder)
    {
        builder.HasKey(p => p.Id);
        builder.HasIndex(p => new { p.TourId, p.PilotId }).IsUnique();
        builder.Ignore(p => p.IsCompleted);

        builder.HasOne(p => p.Tour).WithMany().HasForeignKey(p => p.TourId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(p => p.Pilot).WithMany().HasForeignKey(p => p.PilotId).OnDelete(DeleteBehavior.Cascade);
    }
}