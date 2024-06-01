using DsLauncher.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DsLauncher.Api.Infrastructure;

public class ProductEntityTypeConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.UseTptMappingStrategy();
        builder.Property(x => x.Description).IsRequired();
        builder.Property(x => x.Tags).IsRequired();
        builder.Property(x => x.Name).IsRequired();
        builder.Property(x => x.DeveloperId).IsRequired();
        builder.HasIndex(x => x.Name).IsUnique();
    }
}