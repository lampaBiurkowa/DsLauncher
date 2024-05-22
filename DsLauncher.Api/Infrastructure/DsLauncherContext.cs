using DibBase.Infrastructure;
using DsLauncher.Models;
using Microsoft.EntityFrameworkCore;
using System.Reflection;

namespace DsLauncher.Infrastructure;

public class DsLauncherContext : DibContext
{
    public DbSet<Developer> Developer { get; set; }
    public DbSet<GameActivity> GameActivity { get; set; }
    public DbSet<News> News { get; set; }
    public DbSet<Package> Package { get; set; }
    public DbSet<Product> Product { get; set; }
    public DbSet<Purchase> Purchase { get; set; }
    public DbSet<Review> Review { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseMySql("Server=localhost;Database=DsLauncher;User=root;Password=root;", new MySqlServerVersion(new Version(5, 7, 0)));
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
    }
}