using DsLauncher.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class FollowEntityTypeConfiguration : IEntityTypeConfiguration<Developer>
{
    public void Configure(EntityTypeBuilder<Developer> builder)
    {
        builder.Property(x => x.Description).IsRequired();
        builder.Property(x => x.Name).IsRequired();
        builder.Property(x => x.UserId).IsRequired();
        builder.HasIndex(x => x.Name).IsUnique();
        builder.HasIndex(x => x.UserId).IsUnique();
    }
}