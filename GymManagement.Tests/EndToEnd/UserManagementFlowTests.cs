using System.Net;
using GymManagement.DAL.Data.DbContexts;
using GymManagement.Tests.Support;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace GymManagement.Tests.EndToEnd;

/// <summary>
/// Real HTTP coverage for SuperAdmin-only user management.
/// Requires GYM_TEST_CONNECTION_STRING; otherwise these tests are reported as skipped.
/// </summary>
public sealed class UserManagementFlowTests : IAsyncLifetime
{
    private const string SeedPassword = "Test@12345";
    private const string SuperAdminEmail = "Serag@gmail.com";
    private const string AdminEmail = "Nader@gmail.com";

    private readonly string? _connectionString =
        Environment.GetEnvironmentVariable("GYM_TEST_CONNECTION_STRING");
    private readonly string _databaseName = $"GymManagement_E2E_Users_{Guid.NewGuid():N}";

    private GymTestAppFactory? _factory;

    public async Task InitializeAsync()
    {
        if (_connectionString is null) return;

        await TestDatabase.DropAsync(_connectionString, _databaseName);
        _factory = new GymTestAppFactory(_connectionString, _databaseName, SeedPassword);
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
    public async Task UsersIndex_ListsSeededAccountsForSuperAdmin()
    {
        using var browser = CreateBrowser(RequireFactory());
        await LoginAsync(browser, SuperAdminEmail);

        var html = await browser.GetStringAsync("/Users");

        Assert.Contains("Manage accounts that can access", html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Serag@gmail.com", html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Nader@gmail.com", html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("SuperAdmin", html, StringComparison.OrdinalIgnoreCase);
    }

    [SqlServerFact]
    public async Task CreateUser_AssignsRoleAndAppearsInList()
    {
        var factory = RequireFactory();
        using var browser = CreateBrowser(factory);
        await LoginAsync(browser, SuperAdminEmail);

        var createPage = await browser.GetStringAsync("/Users/Create");
        using var response = await browser.PostFormAsync(
            "/Users/Create",
            new Dictionary<string, string>
            {
                ["FirstName"] = "Power",
                ["LastName"] = "User",
                ["UserName"] = "power.user",
                ["Email"] = "power.user@example.com",
                ["PhoneNumber"] = "01098765432",
                ["Role"] = "Admin",
                ["Password"] = SeedPassword,
                ["__RequestVerificationToken"] = BrowserClient.ExtractToken(createPage),
            });

        AssertRedirect(response, "/Users");
        var index = await browser.GetStringAsync("/Users");
        Assert.Contains("power.user@example.com", index, StringComparison.OrdinalIgnoreCase);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<GymDbContext>();
        var roleName = await (from user in db.Users.AsNoTracking()
                              join userRole in db.UserRoles.AsNoTracking() on user.Id equals userRole.UserId
                              join role in db.Roles.AsNoTracking() on userRole.RoleId equals role.Id
                              where user.Email == "power.user@example.com"
                              select role.Name).SingleAsync();
        Assert.Equal("Admin", roleName);
    }

    [SqlServerFact]
    public async Task UsersPage_RedirectsAdminToAccessDenied()
    {
        using var browser = CreateBrowser(RequireFactory());
        await LoginAsync(browser, AdminEmail);

        using var response = await browser.GetAsync("/Users");

        AssertRedirect(response, "/Account/AccessDenied");
    }

    [SqlServerFact]
    public async Task Edit_CannotDemoteTheLastSuperAdmin()
    {
        var factory = RequireFactory();
        using var browser = CreateBrowser(factory);
        await LoginAsync(browser, SuperAdminEmail);

        string superAdminId;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<GymDbContext>();
            superAdminId = await db.Users
                .AsNoTracking()
                .Where(u => u.Email == SuperAdminEmail)
                .Select(u => u.Id)
                .SingleAsync();
        }

        var editPage = await browser.GetStringAsync($"/Users/Edit/{superAdminId}");
        using var response = await browser.PostFormAsync(
            "/Users/Edit",
            new Dictionary<string, string>
            {
                ["Id"] = superAdminId,
                ["FirstName"] = "Serag",
                ["LastName"] = "Ibrahim",
                ["UserName"] = "Sergio",
                ["Email"] = SuperAdminEmail,
                ["PhoneNumber"] = "01123456789",
                ["Role"] = "Admin",
                ["__RequestVerificationToken"] = BrowserClient.ExtractToken(editPage),
            });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("At least one SuperAdmin must remain", body, StringComparison.OrdinalIgnoreCase);

        using var verifyScope = factory.Services.CreateScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<GymDbContext>();
        var roleName = await (from user in verifyDb.Users.AsNoTracking()
                              join userRole in verifyDb.UserRoles.AsNoTracking() on user.Id equals userRole.UserId
                              join role in verifyDb.Roles.AsNoTracking() on userRole.RoleId equals role.Id
                              where user.Email == SuperAdminEmail
                              select role.Name).SingleAsync();
        Assert.Equal("SuperAdmin", roleName);
    }

    private GymTestAppFactory RequireFactory() =>
        _factory ?? throw new InvalidOperationException(
            "Set GYM_TEST_CONNECTION_STRING to run SQL Server end-to-end tests.");

    private static BrowserClient CreateBrowser(GymTestAppFactory factory) =>
        new(factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
        }));

    private static async Task LoginAsync(BrowserClient browser, string email)
    {
        var loginPage = await browser.GetStringAsync("/Account/Login");
        using var response = await browser.PostFormAsync(
            "/Account/Login",
            new Dictionary<string, string>
            {
                ["Email"] = email,
                ["Password"] = SeedPassword,
                ["RememberMe"] = "false",
                ["__RequestVerificationToken"] = BrowserClient.ExtractToken(loginPage),
            });

        AssertRedirect(response, "/Home");
    }

    private static void AssertRedirect(HttpResponseMessage response, string expectedPath)
    {
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        var location = response.Headers.Location?.OriginalString;
        Assert.False(string.IsNullOrEmpty(location), "Redirect response has no Location header.");
        Assert.Contains(expectedPath, location, StringComparison.OrdinalIgnoreCase);
    }
}
