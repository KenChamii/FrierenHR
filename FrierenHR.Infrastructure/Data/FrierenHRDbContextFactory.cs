using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace FrierenHR.Infrastructure.Data;

/// <summary>Builds a context for design-time tooling: "dotnet ef migrations add", "database update".</summary>
public class FrierenHRDbContextFactory : IDesignTimeDbContextFactory<FrierenHRDbContext>
{
    /// <summary>The database docker-compose.dev.yml starts.</summary>
    private const string LocalDevelopmentConnectionString =
        "Host=localhost;Port=5433;Database=frierenhr;Username=frierenhr;Password=localdev";

    public FrierenHRDbContext CreateDbContext(string[] args)
    {
        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";

        // The connection string lives with the host, which is a sibling of this project.
        var webApiPath = Path.Combine(Directory.GetCurrentDirectory(), "..", "FrierenHR.WebAPI");

        var configuration = new ConfigurationBuilder()
            .SetBasePath(webApiPath)
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
            .AddJsonFile($"appsettings.{environment}.json", optional: true, reloadOnChange: false)
            .AddEnvironmentVariables()
            .Build();

        // User secrets are deliberately not read here.
        var configured = configuration.GetConnectionString("DefaultConnection");

        var connectionString = string.IsNullOrWhiteSpace(configured)
            ? LocalDevelopmentConnectionString
            : configured;

        var optionsBuilder = new DbContextOptionsBuilder<FrierenHRDbContext>();
        optionsBuilder.UseNpgsql(connectionString);

        return new FrierenHRDbContext(optionsBuilder.Options);
    }
}
