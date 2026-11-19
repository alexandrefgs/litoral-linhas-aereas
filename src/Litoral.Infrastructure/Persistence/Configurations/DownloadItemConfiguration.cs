using Litoral.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Litoral.Infrastructure.Persistence.Configurations;

public sealed class DownloadItemConfiguration : IEntityTypeConfiguration<DownloadItem>
{
    public void Configure(EntityTypeBuilder<DownloadItem> builder)
    {
        builder.HasKey(d => d.Id);
        builder.Property(d => d.Title).HasMaxLength(DownloadItem.MaxTitleLength).IsRequired();
        builder.Property(d => d.Description).HasMaxLength(DownloadItem.MaxDescriptionLength);
        builder.Property(d => d.Category).HasConversion<string>().HasMaxLength(20);
        builder.Property(d => d.AircraftType).HasMaxLength(4);
        builder.Property(d => d.Version).HasMaxLength(DownloadItem.MaxVersionLength);
        builder.Property(d => d.ExternalUrl).HasMaxLength(DownloadItem.MaxUrlLength);
        builder.Property(d => d.StoredName).HasMaxLength(80);
        builder.Property(d => d.FileName).HasMaxLength(DownloadItem.MaxFileNameLength);
        builder.Property(d => d.ContentType).HasMaxLength(100);
        builder.Property(d => d.UpdatedBy).HasMaxLength(150).IsRequired();
        builder.Ignore(d => d.IsFile);
        builder.HasIndex(d => d.Category);
    }
}