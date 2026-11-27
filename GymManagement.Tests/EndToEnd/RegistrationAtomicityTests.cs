using System.Net;
using GymManagement.DAL.Data.DbContexts;
using GymManagement.Tests.Support;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace GymManagement.Tests.EndToEnd;

/// <summary>
/// Verifies that a failed Identity step does not leave a partial registration behind.
/// Requires GYM_TEST_CONNECTION_STRING; otherwise these tests are reported as skipped.
/// </summary>
public sealed class RegistrationAtomicityTests : IAsyncLifetime
{
    private const string Password = "Test@12345";

    private readonly string? _connectionString =
        Environment.GetEnvironmentVariable("GYM_TEST_CONNECTION_STRING");
    private readonly string _databaseName = $"GymManagement_E2E_RegisterAtomic_{Guid.NewGuid():N}";

    private GymTestAppFactory? _factory;

    public async Task InitializeAsync()
    {
        if (_connectionString is null) return;

        await TestDatabase.DropAsync(_connectionString, _databaseName);
        _factory = new GymTestAppFactory(_connectionString, _databaseName, Password);
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
    public async Task Register_MissingMemberRole_RollsBackUserCreation()
    {
        var factory = _factory ?? throw new InvalidOperationException(
            "Set GYM_TEST_CONNECTION_STRING to run SQL Server end-to-end tests.");
        var email = $"atomic-{Guid.NewGuid():N}@example.com";

        using (var scope = factory.Services.CreateScope())
        {
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var memberRole = await roleManager.FindByNameAsync("Member");
            Assert.NotNull(memberRole);
            var deleted = await roleManager.DeleteAsync(memberRole);
            Assert.True(deleted.Succeeded);
        }

        using var browser = new BrowserClient(factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
        }));

        var registerPage = await browser.GetStringAsync("/Account/Register");
        using var response = await browser.PostFormAsync(
            "/Account/Register",
            new Dictionary<string, string>
            {
                ["FirstName"] = "Atomic",
                ["LastName"] = "Member",
                ["Email"] = email,
                ["Phone"] = $"010{Random.Shared.Next(10_000_000, 99_999_999)}",
                ["DateOfBirth"] = "1995-05-20",
                ["Gender"] = "1",
                ["Street"] = "Test Street",
                ["City"] = "Cairo",
                ["BuildingNumber"] = "12",
                ["Password"] = Password,
                ["ConfirmPassword"] = Password,
                ["__RequestVerificationToken"] = BrowserClient.ExtractToken(registerPage),
            });

        var body = await response.Content.ReadAsStringAsync();
        Assert.True(
            response.StatusCode == HttpStatusCode.OK,
            $"Registration returned {(int)response.StatusCode}: {body[..Math.Min(800, body.Length)]}");
        Assert.Contains("Registration could not be completed", body, StringComparison.OrdinalIgnoreCase);

        using var verifyScope = factory.Services.CreateScope();
        var db = verifyScope.ServiceProvider.GetRequiredService<GymDbContext>();
        Assert.False(await db.Users.AnyAsync(user => user.Email == email));
        Assert.False(await db.Members.AnyAsync(member => member.Email == email));
    }
}
