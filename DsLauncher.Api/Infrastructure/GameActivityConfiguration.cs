using DsLauncher.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DsLauncher.Api.Infrastructure;

public class GameActivityEntityTypeConfiguration : IEntityTypeConfiguration<GameActivity>
{
    public void Configure(EntityTypeBuilder<GameActivity> builder)
    {
        builder.Property(x => x.UserGuid).IsRequired();
        builder.Property(x => x.ProductId).IsRequired();
    }
}