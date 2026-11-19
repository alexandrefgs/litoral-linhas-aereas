using Litoral.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Litoral.Infrastructure.Persistence.Configurations;

public sealed class RegistrationBlockConfiguration : IEntityTypeConfiguration<RegistrationBlock>
{
    public void Configure(EntityTypeBuilder<RegistrationBlock> builder)
    {
        builder.HasKey(b => b.Id);
        builder.Property(b => b.EmailHash).HasMaxLength(64).IsRequired();
        builder.HasIndex(b => b.EmailHash);
    }
}