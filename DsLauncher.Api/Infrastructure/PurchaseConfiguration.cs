using DsLauncher.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class PurchaseEntityTypeConfiguration : IEntityTypeConfiguration<Purchase>
{
    public void Configure(EntityTypeBuilder<Purchase> builder)
    {
        builder.Property(x => x.ProductId).IsRequired();
        builder.Property(x => x.Value).IsRequired();
    }
}