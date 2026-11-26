using GymManagementPL;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace GymManagement.Tests.Support;

internal sealed class GymTestAppFactory : WebApplicationFactory<Program>
{
    private readonly Dictionary<string, string?> _settings;

    public GymTestAppFactory(string connectionString, string databaseName, string seedPassword)
    {
        _settings = new Dictionary<string, string?>
        {
            ["ConnectionStrings:DefaultConnection"] =
                TestDatabase.ToTestConnectionString(connectionString, databaseName),
            ["SeedUsers:Password"] = seedPassword,
        };
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration((_, configuration) =>
            configuration.AddInMemoryCollection(_settings));
    }
}
