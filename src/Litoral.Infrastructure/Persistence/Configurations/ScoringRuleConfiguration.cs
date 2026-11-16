using Litoral.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Litoral.Infrastructure.Persistence.Configurations;

public sealed class ScoringRuleConfiguration : IEntityTypeConfiguration<ScoringRule>
{
    public void Configure(EntityTypeBuilder<ScoringRule> builder)
    {
        builder.HasKey(r => r.Id);

        builder.Property(r => r.Category).HasConversion<string>().HasMaxLength(40);
        builder.Property(r => r.Check).HasConversion<string>().HasMaxLength(40);
        builder.Property(r => r.Outcome).HasConversion<string>().HasMaxLength(20);
        builder.Property(r => r.Mode).HasConversion<string>().HasMaxLength(20);
        builder.Property(r => r.Description).HasMaxLength(ScoringRule.MaxDescriptionLength).IsRequired();
        builder.Property(r => r.Text).HasMaxLength(ScoringRule.MaxTextLength);
        builder.Property(r => r.AircraftTypes).HasMaxLength(100);
        builder.Property(r => r.Min).HasPrecision(9, 2);
        builder.Property(r => r.Max).HasPrecision(9, 2);

        builder.Ignore(r => r.AircraftTypeList);

        builder.HasIndex(r => new { r.Category, r.Order });
    }
}