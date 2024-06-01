using DsLauncher.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DsLauncher.Api.Infrastructure;

public class SubscriptionEntityTypeConfiguration : IEntityTypeConfiguration<Subscription>
{
    public void Configure(EntityTypeBuilder<Subscription> builder)
    {
        builder.Property(x => x.DeveloperId).IsRequired();
        builder.Property(x => x.CyclicFeeGuid).IsRequired();
        builder.Property(x => x.UserGuid).IsRequired();
    }
}