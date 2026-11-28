using GymManagement.DAL.Data.DataSeeding;
using GymManagement.DAL.Data.DbContexts;
using GymManagement.DAL.Models;
using GymManagement.Tests.Support;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace GymManagement.Tests.EndToEnd;

/// <summary>
/// Verifies the opt-in demo dataset against a disposable SQL Server database.
/// Requires GYM_TEST_CONNECTION_STRING; otherwise the test is reported as skipped.
/// </summary>
public sealed class DemoDataSeedingTests : IAsyncLifetime
{
    private const string Password = "Test@12345";

    private readonly string? _connectionString =
        Environment.GetEnvironmentVariable("GYM_TEST_CONNECTION_STRING");
    private readonly string _databaseName = $"GymManagement_E2E_DemoSeed_{Guid.NewGuid():N}";

    private GymTestAppFactory? _factory;

    public async Task InitializeAsync()
    {
        if (_connectionString is null) return;

        await TestDatabase.DropAsync(_connectionString, _databaseName);
        _factory = new GymTestAppFactory(
            _connectionString,
            _databaseName,
            Password,
            new Dictionary<string, string?>
            {
                ["SeedDemoData:Enabled"] = "true",
            });

        using var startupClient = _factory.CreateClient();
    }

    public async Task DisposeAsync()
    {
        _factory?.Dispose();

        if (_connectionString is not null)
        {
            await TestDatabase.DropAsync(_connectionString, _databaseName);
        }
    }

    [SqlServerFact]
    public async Task DemoData_IsSeededAndSecondRunDoesNotDuplicateRows()
    {
        var factory = RequireFactory();
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<GymDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var counts = await ReadCountsAsync(db);
        Assert.Equal(3, counts.Trainers);
        Assert.Equal(3, counts.Members);
        Assert.Equal(3, counts.HealthRecords);
        Assert.Equal(3, counts.HealthRecordAudits);
        Assert.Equal(3, counts.Memberships);
        Assert.Equal(3, counts.Sessions);
        Assert.Equal(2, counts.Bookings);
        Assert.NotNull(await userManager.FindByEmailAsync("demo.member@gymmanagement.local"));

        await DemoDataSeeding.SeedAsync(
            db,
            userManager,
            NullLogger.Instance,
            Password);

        var repeated = await ReadCountsAsync(db);
        Assert.Equal(counts, repeated);
    }

    private static async Task<Counts> ReadCountsAsync(GymDbContext db) => new(
        Trainers: await db.Trainers.CountAsync(),
        Members: await db.Members.CountAsync(),
        HealthRecords: await db.HealthRecords.CountAsync(),
        HealthRecordAudits: await db.HealthRecordAudits.CountAsync(),
        Memberships: await db.Memberships.CountAsync(),
        Sessions: await db.Sessions.CountAsync(),
        Bookings: await db.Bookings.CountAsync());

    private GymTestAppFactory RequireFactory() =>
        _factory ?? throw new InvalidOperationException(
            "Set GYM_TEST_CONNECTION_STRING to run SQL Server end-to-end tests.");

    private sealed record Counts(
        int Trainers,
        int Members,
        int HealthRecords,
        int HealthRecordAudits,
        int Memberships,
        int Sessions,
        int Bookings);
}
