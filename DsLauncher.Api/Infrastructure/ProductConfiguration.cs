using DsLauncher.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class ProductEntityTypeConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.Property(x => x.Description).IsRequired();
        builder.Property(x => x.Tags).IsRequired();
        builder.Property(x => x.Name).IsRequired();
        builder.Property(x => x.DeveloperId).IsRequired();
    }
}