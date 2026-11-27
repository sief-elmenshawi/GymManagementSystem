using System.Net;
using GymManagement.DAL.Data.DbContexts;
using GymManagement.Tests.Support;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace GymManagement.Tests.EndToEnd;

/// <summary>
/// Real HTTP coverage for public registration and its Identity-to-Member link.
/// Requires GYM_TEST_CONNECTION_STRING; otherwise these tests are reported as skipped.
/// </summary>
public sealed class RegistrationFlowTests : IAsyncLifetime
{
    private const string Password = "Test@12345";

    private readonly string? _connectionString =
        Environment.GetEnvironmentVariable("GYM_TEST_CONNECTION_STRING");
    private readonly string _databaseName = $"GymManagement_E2E_Register_{Guid.NewGuid():N}";

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
    public async Task Register_CreatesUserWithOnlyMemberRoleAndLinkedMember()
    {
        var factory = RequireFactory();
        using var browser = CreateBrowser(factory);
        var email = NewEmail();
        var phone = NewPhone();

        var registerPage = await browser.GetStringAsync("/Account/Register");
        Assert.Contains("Create your member account", registerPage, StringComparison.OrdinalIgnoreCase);

        using var response = await browser.PostFormAsync(
            "/Account/Register",
            RegistrationForm(email, phone, BrowserClient.ExtractToken(registerPage)));

        AssertRedirect(response, "/Account/Login");

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<GymDbContext>();

        var user = await db.Users.AsNoTracking().SingleAsync(u => u.Email == email);
        var roles = await (from userRole in db.UserRoles.AsNoTracking()
                           join role in db.Roles.AsNoTracking() on userRole.RoleId equals role.Id
                           where userRole.UserId == user.Id
                           select role.Name!).ToListAsync();

        Assert.Single(roles);
        Assert.Equal("Member", roles[0]);

        var member = await db.Members.AsNoTracking().SingleAsync(m => m.Email == email);
        Assert.Equal(user.Id, member.ApplicationUserId);
        Assert.Equal("Test Member", member.Name);
        Assert.Equal(phone, member.Phone);
        Assert.False(await db.HealthRecords.AsNoTracking().AnyAsync(h => h.MemberId == member.Id));
    }

    [SqlServerFact]
    public async Task Register_DuplicateEmailIsRejected()
    {
        var factory = RequireFactory();
        using var browser = CreateBrowser(factory);
        var email = NewEmail();

        var firstPage = await browser.GetStringAsync("/Account/Register");
        using var firstResponse = await browser.PostFormAsync(
            "/Account/Register",
            RegistrationForm(email, NewPhone(), BrowserClient.ExtractToken(firstPage)));
        AssertRedirect(firstResponse, "/Account/Login");

        var secondPage = await browser.GetStringAsync("/Account/Register");
        using var secondResponse = await browser.PostFormAsync(
            "/Account/Register",
            RegistrationForm(email, NewPhone(), BrowserClient.ExtractToken(secondPage)));

        Assert.Equal(HttpStatusCode.OK, secondResponse.StatusCode);
        var body = await secondResponse.Content.ReadAsStringAsync();
        Assert.Contains("An account with this email already exists", body, StringComparison.OrdinalIgnoreCase);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<GymDbContext>();
        Assert.Equal(1, await db.Users.CountAsync(u => u.Email == email));
        Assert.Equal(1, await db.Members.CountAsync(m => m.Email == email));
    }

    [SqlServerFact]
    public async Task Register_IgnoresAttemptToAssignAdminRole()
    {
        var factory = RequireFactory();
        using var browser = CreateBrowser(factory);
        var email = NewEmail();

        var registerPage = await browser.GetStringAsync("/Account/Register");
        var form = RegistrationForm(email, NewPhone(), BrowserClient.ExtractToken(registerPage));
        form["Role"] = "SuperAdmin";

        using var response = await browser.PostFormAsync("/Account/Register", form);
        AssertRedirect(response, "/Account/Login");

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<GymDbContext>();
        var user = await db.Users.AsNoTracking().SingleAsync(u => u.Email == email);
        var roles = await (from userRole in db.UserRoles.AsNoTracking()
                           join role in db.Roles.AsNoTracking() on userRole.RoleId equals role.Id
                           where userRole.UserId == user.Id
                           select role.Name!).ToListAsync();

        Assert.Equal("Member", Assert.Single(roles));
    }

    [SqlServerFact]
    public async Task RegisteredUser_CanLoginWithNewCredentials()
    {
        var factory = RequireFactory();
        using var registrationBrowser = CreateBrowser(factory);
        var email = NewEmail();

        var registerPage = await registrationBrowser.GetStringAsync("/Account/Register");
        using var registerResponse = await registrationBrowser.PostFormAsync(
            "/Account/Register",
            RegistrationForm(email, NewPhone(), BrowserClient.ExtractToken(registerPage)));
        AssertRedirect(registerResponse, "/Account/Login");

        using var loginBrowser = CreateBrowser(factory);
        await LoginAsync(loginBrowser, email);
    }

    [SqlServerFact]
    public async Task MemberRole_CannotAccessStaffBookingPages()
    {
        var factory = RequireFactory();
        using var registrationBrowser = CreateBrowser(factory);
        var email = NewEmail();

        var registerPage = await registrationBrowser.GetStringAsync("/Account/Register");
        using var registerResponse = await registrationBrowser.PostFormAsync(
            "/Account/Register",
            RegistrationForm(email, NewPhone(), BrowserClient.ExtractToken(registerPage)));
        AssertRedirect(registerResponse, "/Account/Login");

        using var browser = CreateBrowser(factory);
        await LoginAsync(browser, email);

        using var response = await browser.GetAsync("/Booking/Index");
        AssertRedirect(response, "/Account/AccessDenied");
    }

    private GymTestAppFactory RequireFactory() =>
        _factory ?? throw new InvalidOperationException(
            "Set GYM_TEST_CONNECTION_STRING to run SQL Server end-to-end tests.");

    private static BrowserClient CreateBrowser(GymTestAppFactory factory) =>
        new(factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
        }));

    private static Dictionary<string, string> RegistrationForm(
        string email,
        string phone,
        string token) => new()
        {
            ["FirstName"] = "Test",
            ["LastName"] = "Member",
            ["Email"] = email,
            ["Phone"] = phone,
            ["DateOfBirth"] = "1995-05-20",
            ["Gender"] = "1",
            ["Street"] = "Test Street",
            ["City"] = "Cairo",
            ["BuildingNumber"] = "12",
            ["Password"] = Password,
            ["ConfirmPassword"] = Password,
            ["__RequestVerificationToken"] = token,
        };

    private static async Task LoginAsync(BrowserClient browser, string email)
    {
        var loginPage = await browser.GetStringAsync("/Account/Login");
        using var response = await browser.PostFormAsync(
            "/Account/Login",
            new Dictionary<string, string>
            {
                ["Email"] = email,
                ["Password"] = Password,
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

    private static string NewEmail() => $"member-{Guid.NewGuid():N}@example.com";

    private static string NewPhone() => $"010{Random.Shared.Next(10_000_000, 99_999_999)}";
}
