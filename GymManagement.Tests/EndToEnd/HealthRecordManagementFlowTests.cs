using System.Net;
using GymManagement.DAL.Data.DbContexts;
using GymManagement.Tests.Support;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace GymManagement.Tests.EndToEnd;

/// <summary>
/// Real HTTP coverage for adding and editing health records after registration.
/// Requires GYM_TEST_CONNECTION_STRING; otherwise these tests are reported as skipped.
/// </summary>
public sealed class HealthRecordManagementFlowTests : IAsyncLifetime
{
    private const string Password = "Test@12345";
    private const string SuperAdminEmail = "Serag@gmail.com";

    private readonly string? _connectionString =
        Environment.GetEnvironmentVariable("GYM_TEST_CONNECTION_STRING");
    private readonly string _databaseName = $"GymManagement_E2E_Health_{Guid.NewGuid():N}";

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
    public async Task StaffCanCreateAndEditHealthRecordForRegisteredMember()
    {
        var factory = RequireFactory();
        var email = $"health.member-{Guid.NewGuid():N}@example.com";
        var memberId = await RegisterMemberAsync(factory, email);

        using var browser = CreateBrowser(factory);
        await LoginAsync(browser, SuperAdminEmail);

        var createPage = await browser.GetStringAsync($"/Members/HealthRecordCreate/{memberId}");
        Assert.Contains("Add Health Record", createPage, StringComparison.OrdinalIgnoreCase);

        using var createResponse = await browser.PostFormAsync(
            $"/Members/HealthRecordCreate/{memberId}",
            HealthForm(createPage, "175.5", "70.5", "O+", "created after registration"));

        AssertRedirect(createResponse, $"/Members/HealthRecordDetails/{memberId}");

        var details = await browser.GetStringAsync($"/Members/HealthRecordDetails/{memberId}");
        Assert.Contains("Health Record", details, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("created after registration", details, StringComparison.OrdinalIgnoreCase);

        AssertHealthRecord(factory, memberId, 175.5m, 70.5m, "O+", "created after registration");

        var editPage = await browser.GetStringAsync($"/Members/HealthRecordEdit/{memberId}");
        using var editResponse = await browser.PostFormAsync(
            $"/Members/HealthRecordEdit/{memberId}",
            HealthForm(editPage, "176.25", "72.75", "A+", "updated by staff"));

        AssertRedirect(editResponse, $"/Members/HealthRecordDetails/{memberId}");
        AssertHealthRecord(factory, memberId, 176.25m, 72.75m, "A+", "updated by staff");
    }

    [SqlServerFact]
    public async Task InvalidBloodType_DoesNotCreateHealthRecord()
    {
        var factory = RequireFactory();
        var email = $"health.invalid-{Guid.NewGuid():N}@example.com";
        var memberId = await RegisterMemberAsync(factory, email);

        using var browser = CreateBrowser(factory);
        await LoginAsync(browser, SuperAdminEmail);

        var createPage = await browser.GetStringAsync($"/Members/HealthRecordCreate/{memberId}");
        using var response = await browser.PostFormAsync(
            $"/Members/HealthRecordCreate/{memberId}",
            HealthForm(createPage, "175", "70", string.Empty, "invalid"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<GymDbContext>();
        Assert.False(await db.HealthRecords.AnyAsync(record => record.MemberId == memberId));
    }

    private static Dictionary<string, string> HealthForm(
        string page,
        string height,
        string weight,
        string bloodType,
        string note) => new()
        {
            ["Height"] = height,
            ["Weight"] = weight,
            ["BloodType"] = bloodType,
            ["Note"] = note,
            ["__RequestVerificationToken"] = BrowserClient.ExtractToken(page),
        };

    private static async Task<int> RegisterMemberAsync(GymTestAppFactory factory, string email)
    {
        using var browser = CreateBrowser(factory);
        var registerPage = await browser.GetStringAsync("/Account/Register");
        using var response = await browser.PostFormAsync(
            "/Account/Register",
            new Dictionary<string, string>
            {
                ["FirstName"] = "Health",
                ["LastName"] = "Member",
                ["Email"] = email,
                ["Phone"] = $"010{Random.Shared.Next(10_000_000, 99_999_999)}",
                ["DateOfBirth"] = "1994-04-12",
                ["Gender"] = "2",
                ["Street"] = "Health Street",
                ["City"] = "Alexandria",
                ["BuildingNumber"] = "5",
                ["Password"] = Password,
                ["ConfirmPassword"] = Password,
                ["__RequestVerificationToken"] = BrowserClient.ExtractToken(registerPage),
            });
        AssertRedirect(response, "/Account/Login");

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<GymDbContext>();
        return await db.Members.AsNoTracking()
            .Where(member => member.Email == email)
            .Select(member => member.Id)
            .SingleAsync();
    }

    private static void AssertHealthRecord(
        GymTestAppFactory factory,
        int memberId,
        decimal height,
        decimal weight,
        string bloodType,
        string note)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<GymDbContext>();
        var record = db.HealthRecords.AsNoTracking().Single(record => record.MemberId == memberId);
        Assert.Equal(height, record.Height);
        Assert.Equal(weight, record.Weight);
        Assert.Equal(bloodType, record.BloodType);
        Assert.Equal(note, record.Note);
    }

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

    private GymTestAppFactory RequireFactory() =>
        _factory ?? throw new InvalidOperationException(
            "Set GYM_TEST_CONNECTION_STRING to run SQL Server end-to-end tests.");

    private static BrowserClient CreateBrowser(GymTestAppFactory factory) =>
        new(factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
        }));

    private static void AssertRedirect(HttpResponseMessage response, string expectedPath)
    {
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        var location = response.Headers.Location?.OriginalString;
        Assert.False(string.IsNullOrEmpty(location), "Redirect response has no Location header.");
        Assert.Contains(expectedPath, location, StringComparison.OrdinalIgnoreCase);
    }
}
