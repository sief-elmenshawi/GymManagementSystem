using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace GymManagement.DAL.Data.DbContexts;

/// <summary>
/// Design-time factory so EF Core commands never start the web host (and therefore
/// never require an AutoMapper license key or seed password).
/// </summary>
public sealed class GymDbContextFactory : IDesignTimeDbContextFactory<GymDbContext>
{
    public GymDbContext CreateDbContext(string[] args)
    {
        var root = Directory.GetCurrentDirectory();
        var configuration = new ConfigurationBuilder()
            .AddJsonFile(Path.Combine(root, "GymManagement", "appsettings.json"), optional: true)
            .AddJsonFile(Path.Combine(root, "GymManagement", "appsettings.Development.json"), optional: true)
            .AddJsonFile(Path.Combine(root, "appsettings.json"), optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Set ConnectionStrings__DefaultConnection (or a local appsettings.json) before running EF Core tools.");
        }

        var options = new DbContextOptionsBuilder<GymDbContext>()
            .UseSqlServer(connectionString)
            .Options;

        return new GymDbContext(options);
    }
}
