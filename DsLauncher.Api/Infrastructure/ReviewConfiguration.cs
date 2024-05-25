using DsLauncher.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DsLauncher.Api.Infrastructure;

public class ReviewEntityTypeConfiguration : IEntityTypeConfiguration<Review>
{
    public void Configure(EntityTypeBuilder<Review> builder)
    {
        builder.Property(x => x.ProductId).IsRequired();
        builder.Property(x => x.UserGuid).IsRequired();
        builder.Property(x => x.Rate).IsRequired();
    }
}