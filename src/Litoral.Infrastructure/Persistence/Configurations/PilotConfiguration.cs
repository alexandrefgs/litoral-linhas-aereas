using Litoral.Domain.Entities;
using Litoral.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Litoral.Domain.Enums;

namespace Litoral.Infrastructure.Persistence.Configurations;

public sealed class PilotConfiguration : IEntityTypeConfiguration<Pilot>
{
    public void Configure(EntityTypeBuilder<Pilot> builder)
    {
        builder.HasKey(p => p.Id);
        builder.HasIndex(p => p.PilotNumber).IsUnique();
        builder.HasIndex(p => p.UserId).IsUnique();
        builder.Property(p => p.FirstName).HasMaxLength(60).IsRequired();
        builder.Property(p => p.LastName).HasMaxLength(60).IsRequired();
        builder.Property(p => p.BaseIcao).HasMaxLength(4).IsRequired();
        builder.Property(p => p.CurrentLocationIcao).HasMaxLength(4).IsRequired();
        builder.Property(p => p.SimBriefUsername).HasMaxLength(60);
        builder.Property(p => p.CountryCode).HasMaxLength(2);
        builder.Property(p => p.State).HasMaxLength(60);
        builder.Property(p => p.City).HasMaxLength(80);
        builder.Property(p => p.VatsimId).HasMaxLength(10);
        builder.Property(p => p.IvaoId).HasMaxLength(10);
        builder.Property(p => p.DiscordId).HasMaxLength(20);
        builder.Property(p => p.PhotoUrl).HasMaxLength(300);
        builder.Property(p => p.CoverUrl).HasMaxLength(300);
        builder.Property(p => p.SimAircraftTypes).HasMaxLength(200);
        builder.Property(p => p.NameFormat)
            .HasConversion<string>()
            .HasMaxLength(30)
            .HasDefaultValue(NameFormat.FirstInitialAndLast);
        builder.Property(p => p.CanJumpseat).HasDefaultValue(true);
        builder.Property(p => p.CanTransferAircraft).HasDefaultValue(true);
        builder.HasIndex(p => p.VatsimId).IsUnique().HasFilter("[VatsimId] IS NOT NULL");
        builder.HasIndex(p => p.IvaoId).IsUnique().HasFilter("[IvaoId] IS NOT NULL");

        builder.Ignore(p => p.DisplayId);
        builder.Ignore(p => p.NetworkCallsign);
        builder.Ignore(p => p.PublicName);
        builder.Ignore(p => p.SimAircraftList);

        builder.HasOne(p => p.Rank).WithMany().HasForeignKey(p => p.RankId).OnDelete(DeleteBehavior.Restrict);
        builder.Navigation(p => p.PointTransactions).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.HasOne<ApplicationUser>().WithOne().HasForeignKey<Pilot>(p => p.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}