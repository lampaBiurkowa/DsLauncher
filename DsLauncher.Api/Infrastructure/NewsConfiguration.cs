using DsLauncher.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DsLauncher.Api.Infrastructure;

public class NewsEntityTypeConfiguration : IEntityTypeConfiguration<News>
{
    public void Configure(EntityTypeBuilder<News> builder)
    {
        builder.Property(x => x.Content).IsRequired();
        builder.Property(x => x.Image).IsRequired();
        builder.Property(x => x.Summary).IsRequired();
        builder.Property(x => x.Title).IsRequired();
    }
}