using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using System.IO;

namespace FrierenHR.Infrastructure.Data;

public class FrierenHRDbContextFactory : IDesignTimeDbContextFactory<FrierenHRDbContext>
{
    public FrierenHRDbContext CreateDbContext(string[] args)
    {
        // Point to the WebAPI project's appsettings.json since that's where the connection string lives
        var basePath = Path.Combine(Directory.GetCurrentDirectory(), "..", "FrierenHR.WebAPI");

        var configuration = new ConfigurationBuilder()
            .SetBasePath(basePath)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
            .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")}.json", optional: true)
            .Build();

        var connectionString = configuration.GetConnectionString("DefaultConnection");

        var optionsBuilder = new DbContextOptionsBuilder<FrierenHRDbContext>();
        optionsBuilder.UseSqlServer(connectionString);

        return new FrierenHRDbContext(optionsBuilder.Options);
    }
}