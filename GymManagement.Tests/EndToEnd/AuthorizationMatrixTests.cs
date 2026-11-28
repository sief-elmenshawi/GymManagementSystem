using System.Net;
using GymManagement.Tests.Support;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace GymManagement.Tests.EndToEnd;

/// <summary>
/// Explicit role matrix for the seeded accounts and a registered member.
/// Requires GYM_TEST_CONNECTION_STRING; otherwise these tests are reported as skipped.
/// </summary>
public sealed class AuthorizationMatrixTests : IAsyncLifetime
{
    private const string SeedPassword = "Test@12345";
    private const string SuperAdminEmail = "Serag@gmail.com";
    private const string AdminEmail = "Nader@gmail.com";

    private static readonly string[] SuperAdminOnlyPaths = ["/Dashboard", "/Users", "/Members/Index"];
    private static readonly string[] StaffPaths =
        ["/Sessions/Index", "/Trainers/Index", "/Membership/Index", "/Booking/Index", "/Plans/Index"];
    private static readonly string[] MemberOnlyPaths = ["/Member"];
    private static readonly string[] PublicPaths =
        ["/Account/Login", "/Account/Register", "/Account/ForgotPassword", "/Account/ResendEmailConfirmation"];

    private readonly string? _connectionString =
        Environment.GetEnvironmentVariable("GYM_TEST_CONNECTION_STRING");
    private readonly string _databaseName = $"GymManagement_E2E_Roles_{Guid.NewGuid():N}";

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
    public async Task Anonymous_CanReachPublicPages_ButNotProtectedPages()
    {
        using var browser = CreateBrowser(RequireFactory());

        foreach (var path in PublicPaths)
        {
            using var response = await browser.GetAsync(path);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        foreach (var path in SuperAdminOnlyPaths.Concat(StaffPaths).Concat(MemberOnlyPaths).Append("/Home"))
        {
            using var response = await browser.GetAsync(path);
            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
            Assert.Contains(
                "/Account/Login",
                response.Headers.Location?.OriginalString ?? string.Empty,
                StringComparison.OrdinalIgnoreCase);
        }
    }

    [SqlServerFact]
    public async Task SuperAdmin_CanReachEveryProtectedArea()
    {
        using var browser = await SignedInBrowserAsync(SuperAdminEmail);

        using var home = await browser.GetAsync("/Home");
        AccountTestFlows.AssertRedirect(home, "/Dashboard");

        foreach (var path in SuperAdminOnlyPaths.Concat(StaffPaths))
        {
            using var response = await browser.GetAsync(path);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
    }

    [SqlServerFact]
    public async Task Admin_CanReachStaffPages_ButNotSuperAdminOrMemberPages()
    {
        using var browser = await SignedInBrowserAsync(AdminEmail);

        using var home = await browser.GetAsync("/Home");
        Assert.Equal(HttpStatusCode.OK, home.StatusCode);

        foreach (var path in StaffPaths)
        {
            using var response = await browser.GetAsync(path);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        foreach (var path in SuperAdminOnlyPaths.Concat(MemberOnlyPaths))
        {
            using var response = await browser.GetAsync(path);
            AccountTestFlows.AssertRedirect(response, "/Account/AccessDenied");
        }
    }

    [SqlServerFact]
    public async Task Member_CanReachPortal_ButNotStaffOrSuperAdminPages()
    {
        var email = await CreateConfirmedMemberAsync();
        using var browser = await SignedInBrowserAsync(email);

        using var home = await browser.GetAsync("/Home");
        AccountTestFlows.AssertRedirect(home, "/Member");

        foreach (var path in MemberOnlyPaths)
        {
            using var response = await browser.GetAsync(path);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        foreach (var path in SuperAdminOnlyPaths.Concat(StaffPaths))
        {
            using var response = await browser.GetAsync(path);
            AccountTestFlows.AssertRedirect(response, "/Account/AccessDenied");
        }
    }

    private GymTestAppFactory RequireFactory() =>
        _factory ?? throw new InvalidOperationException(
            "Set GYM_TEST_CONNECTION_STRING to run SQL Server end-to-end tests.");

    private static BrowserClient CreateBrowser(GymTestAppFactory factory) =>
        new(factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
        }));

    private async Task<BrowserClient> SignedInBrowserAsync(string email)
    {
        var browser = CreateBrowser(RequireFactory());
        await AccountTestFlows.LoginAsync(browser, email, SeedPassword);
        return browser;
    }

    private async Task<string> CreateConfirmedMemberAsync()
    {
        var email = $"matrix.member-{Guid.NewGuid():N}@example.com";
        var phone = "010" + Random.Shared.Next(10_000_000, 99_999_999).ToString("D8");

        using var browser = CreateBrowser(RequireFactory());
        await AccountTestFlows.RegisterAsync(browser, email, phone, SeedPassword);
        await AccountTestFlows.ConfirmEmailAsync(browser, email);
        return email;
    }
}
