using Litoral.Domain.Entities;
using Litoral.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Litoral.Infrastructure.Persistence.Configurations;

public sealed class PilotApplicationConfiguration : IEntityTypeConfiguration<PilotApplication>
{
    public void Configure(EntityTypeBuilder<PilotApplication> builder)
    {
        builder.HasKey(a => a.Id);

        builder.Property(a => a.FirstName).HasMaxLength(PilotApplication.MaxNameLength).IsRequired();
        builder.Property(a => a.LastName).HasMaxLength(PilotApplication.MaxNameLength).IsRequired();
        builder.Property(a => a.HubIcao).HasMaxLength(4).IsRequired();
        builder.Property(a => a.VatsimId).HasMaxLength(10);
        builder.Property(a => a.IvaoId).HasMaxLength(10);
        builder.Property(a => a.Simulator).HasMaxLength(20).IsRequired();
        builder.Property(a => a.AddOns).HasMaxLength(PilotApplication.MaxAddOnsLength).IsRequired();
        builder.Property(a => a.Motivation).HasMaxLength(PilotApplication.MaxMotivationLength).IsRequired();
        builder.Property(a => a.AcceptedTermsVersion).HasMaxLength(20).IsRequired();
        builder.Ignore(a => a.AddOnList);

        builder.HasOne<ApplicationUser>().WithOne().HasForeignKey<PilotApplication>(a => a.UserId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(a => a.CreatedAt);
    }
}