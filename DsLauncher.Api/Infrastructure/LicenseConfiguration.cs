using DsLauncher.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DsLauncher.Api.Infrastructure;

public class LicenseEntityTypeConfiguration : IEntityTypeConfiguration<License>
{
    public void Configure(EntityTypeBuilder<License> builder)
    {
        builder.Property(x => x.Salt).IsRequired();
        builder.Property(x => x.DeveloperId).IsRequired();
        builder.Property(x => x.Key).IsRequired();
    }
}
