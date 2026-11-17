using Litoral.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Litoral.Infrastructure.Persistence.Configurations;

public sealed class MonthlyAwardConfiguration : IEntityTypeConfiguration<MonthlyAward>
{
    public void Configure(EntityTypeBuilder<MonthlyAward> builder)
    {
        builder.HasKey(a => a.Id);

        builder.Property(a => a.Category).HasConversion<string>().HasMaxLength(20);
        builder.Property(a => a.PilotName).HasMaxLength(150).IsRequired();

        builder.HasOne(a => a.Pilot)
            .WithMany()
            .HasForeignKey(a => a.PilotId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(a => new { a.Year, a.Month, a.Category, a.Position }).IsUnique();
    }
}