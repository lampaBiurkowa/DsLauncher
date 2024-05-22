using DsLauncher.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class GameActivityEntityTypeConfiguration : IEntityTypeConfiguration<GameActivity>
{
    public void Configure(EntityTypeBuilder<GameActivity> builder)
    {
        builder.Property(x => x.UserGuid).IsRequired();
    }
}