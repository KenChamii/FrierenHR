using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace FrierenHR.Infrastructure.Data;

/// <summary>
/// Builds a context for design-time tooling: "dotnet ef migrations add", "database update",
/// and the migration bundle the deploy runs.
/// </summary>
/// <remarks>
/// The tooling runs without the WebAPI host, so the configuration it would normally get from
/// the host has to be rebuilt here. Sources are read in the order the runtime uses, with the
/// environment last so a deploy can supply the connection string without a file on disk.
/// </remarks>
public class FrierenHRDbContextFactory : IDesignTimeDbContextFactory<FrierenHRDbContext>
{
    /// <summary>
    /// The database docker-compose.dev.yml starts. Hard-coded on purpose: these are the same
    /// values that file already declares in plain text, so repeating them here exposes
    /// nothing, and it means "dotnet ef database update" works on a fresh clone with no
    /// setup. A deployed environment never reaches this line, because it supplies
    /// ConnectionStrings__DefaultConnection and the lookup above succeeds.
    /// </summary>
    private const string LocalDevelopmentConnectionString =
        "Host=localhost;Port=5433;Database=frierenhr;Username=frierenhr;Password=localdev";

    public FrierenHRDbContext CreateDbContext(string[] args)
    {
        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";

        // The connection string lives with the host, which is a sibling of this project. Marked
        // optional because a deploy has no appsettings file and supplies the value by environment.
        var webApiPath = Path.Combine(Directory.GetCurrentDirectory(), "..", "FrierenHR.WebAPI");

        var configuration = new ConfigurationBuilder()
            .SetBasePath(webApiPath)
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
            .AddJsonFile($"appsettings.{environment}.json", optional: true, reloadOnChange: false)
            .AddEnvironmentVariables()
            .Build();

        // User secrets are deliberately not read here. They are keyed to the WebAPI assembly,
        // and reaching them from this project would mean carrying its identifier in two
        // places. The local fallback below covers the same case without that duplication.
        //
        // Tested for blank rather than null: appsettings.json ships the key with an empty
        // value as a placeholder, so the key is always present and a null check would never
        // fire. An empty string reaches Npgsql as a connection string with nothing in it.
        var configured = configuration.GetConnectionString("DefaultConnection");

        var connectionString = string.IsNullOrWhiteSpace(configured)
            ? LocalDevelopmentConnectionString
            : configured;

        var optionsBuilder = new DbContextOptionsBuilder<FrierenHRDbContext>();
        optionsBuilder.UseNpgsql(connectionString);

        return new FrierenHRDbContext(optionsBuilder.Options);
    }
}
