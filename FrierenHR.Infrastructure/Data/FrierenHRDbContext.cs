using FrierenHR.Core.Entities;
using Microsoft.EntityFrameworkCore;
using System.Reflection.Emit;

namespace FrierenHR.Infrastructure.Data;

public class FrierenHRDbContext : DbContext
{
    public FrierenHRDbContext(DbContextOptions<FrierenHRDbContext> options) : base(options) { }

    public DbSet<AppConfig> AppConfigs => Set<AppConfig>();
    public DbSet<Company> Companies => Set<Company>();
    // Add each new DbSet here as you create the entity in later phases —
    // this is the ONE file you touch every phase, everything else is additive.

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(FrierenHRDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}