using DsLauncher.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DsLauncher.Api.Infrastructure;

public class PurchaseEntityTypeConfiguration : IEntityTypeConfiguration<Purchase>
{
    public void Configure(EntityTypeBuilder<Purchase> builder)
    {
        builder.Property(x => x.ProductId).IsRequired();
        builder.Property(x => x.TransactionGuid).IsRequired();
        builder.Property(x => x.UserGuid).IsRequired();
    }
}