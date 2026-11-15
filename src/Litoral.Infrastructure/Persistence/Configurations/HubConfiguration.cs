using Litoral.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Litoral.Infrastructure.Persistence.Configurations;

public sealed class HubConfiguration : IEntityTypeConfiguration<Hub>
{
    public void Configure(EntityTypeBuilder<Hub> builder)
    {
        builder.HasKey(h => h.Id);
        builder.HasIndex(h => h.AirportId).IsUnique();
        builder.HasOne(h => h.Airport).WithMany().HasForeignKey(h => h.AirportId).OnDelete(DeleteBehavior.Restrict);
    }
}