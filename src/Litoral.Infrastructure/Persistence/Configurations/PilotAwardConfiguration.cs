using Litoral.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Litoral.Infrastructure.Persistence.Configurations;

public sealed class PilotAwardConfiguration : IEntityTypeConfiguration<PilotAward>
{
    public void Configure(EntityTypeBuilder<PilotAward> builder)
    {
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Note).HasMaxLength(PilotAward.MaxNoteLength);
        builder.Property(a => a.GrantedBy).HasMaxLength(150).IsRequired();
        builder.HasIndex(a => new { a.PilotId, a.AwardId }).IsUnique();

        builder.HasOne(a => a.Pilot).WithMany().HasForeignKey(a => a.PilotId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(a => a.Award).WithMany().HasForeignKey(a => a.AwardId).OnDelete(DeleteBehavior.Restrict);
    }
}