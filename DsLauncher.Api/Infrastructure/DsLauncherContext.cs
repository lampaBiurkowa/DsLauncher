using DibBase.Infrastructure;
using DibBase.Options;
using DsLauncher.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Reflection;

namespace DsLauncher.Infrastructure;

public class DsLauncherContext(IOptions<DsDbLibOptions> options) : DibContext
{
    public DbSet<Developer> Developer { get; set; }
    public DbSet<Activity> Activity { get; set; }
    public DbSet<News> News { get; set; }
    public DbSet<Package> Package { get; set; }
    public DbSet<Product> Product { get; set; }
    public DbSet<Game> Game { get; set; }
    public DbSet<App> App { get; set; }
    public DbSet<Purchase> Purchase { get; set; }
    public DbSet<Review> Review { get; set; }
    public DbSet<License> License { get; set; }
    public DbSet<Subscription> Subscription { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseMySql($"Server={options.Value.Host};Database={options.Value.DatabaseName};User={options.Value.User};Password={options.Value.Password};SSL Mode=None",
        new MySqlServerVersion(new Version(5, 7, 0)));
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
    }
}