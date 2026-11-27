using System.Net;
using GymManagement.DAL.Data.DbContexts;
using GymManagement.DAL.Models;
using GymManagement.DAL.Models.Enums;
using GymManagement.Tests.Support;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace GymManagement.Tests.EndToEnd;

/// <summary>
/// Real HTTP coverage for the post-login Member area.
/// Requires GYM_TEST_CONNECTION_STRING; otherwise these tests are reported as skipped.
/// </summary>
public sealed class MemberPortalFlowTests : IAsyncLifetime
{
    private const string Password = "Test@12345";
    private const string SuperAdminEmail = "Serag@gmail.com";

    private readonly string? _connectionString =
        Environment.GetEnvironmentVariable("GYM_TEST_CONNECTION_STRING");
    private readonly string _databaseName = $"GymManagement_E2E_Portal_{Guid.NewGuid():N}";

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
    public async Task MemberLogin_RedirectsToPortalAndShowsOnlyOwnBookings()
    {
        var factory = RequireFactory();
        var email = $"portal.member-{Guid.NewGuid():N}@example.com";
        await RegisterMemberAsync(factory, email);

        using var browser = CreateBrowser(factory);
        await LoginAsync(browser, email);

        using var homeResponse = await browser.GetAsync("/Home");
        AssertRedirect(homeResponse, "/Member");

        var emptyPortal = await browser.GetStringAsync("/Member");
        Assert.Contains("My Gym Profile", emptyPortal, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(email, emptyPortal, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("No bookings yet", emptyPortal, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("My Profile", emptyPortal, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Sessions Schedule", emptyPortal, StringComparison.OrdinalIgnoreCase);

        await SeedBookingsAsync(factory, email);

        var portal = await browser.GetStringAsync("/Member");
        Assert.Contains("My portal session", portal, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Portal Yoga", portal, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Portal Trainer", portal, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Other member session", portal, StringComparison.OrdinalIgnoreCase);
    }

    [SqlServerFact]
    public async Task MemberAccountWithoutLinkedProfile_ShowsHelpfulNotice()
    {
        var factory = RequireFactory();
        var email = $"unlinked.member-{Guid.NewGuid():N}@example.com";

        using var adminBrowser = CreateBrowser(factory);
        await LoginAsync(adminBrowser, SuperAdminEmail);
        var createPage = await adminBrowser.GetStringAsync("/Users/Create");
        using var createResponse = await adminBrowser.PostFormAsync(
            "/Users/Create",
            new Dictionary<string, string>
            {
                ["FirstName"] = "Unlinked",
                ["LastName"] = "Member",
                ["UserName"] = email,
                ["Email"] = email,
                ["PhoneNumber"] = "01012345678",
                ["Role"] = "Member",
                ["Password"] = Password,
                ["__RequestVerificationToken"] = BrowserClient.ExtractToken(createPage),
            });
        AssertRedirect(createResponse, "/Users");

        using var browser = CreateBrowser(factory);
        await LoginAsync(browser, email);
        var portal = await browser.GetStringAsync("/Member");

        Assert.Contains("No member profile is linked", portal, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(email, portal, StringComparison.OrdinalIgnoreCase);
    }

    private static async Task RegisterMemberAsync(GymTestAppFactory factory, string email)
    {
        using var browser = CreateBrowser(factory);
        var registerPage = await browser.GetStringAsync("/Account/Register");
        using var response = await browser.PostFormAsync(
            "/Account/Register",
            new Dictionary<string, string>
            {
                ["FirstName"] = "Portal",
                ["LastName"] = "Member",
                ["Email"] = email,
                ["Phone"] = $"010{Random.Shared.Next(10_000_000, 99_999_999)}",
                ["DateOfBirth"] = "1996-08-15",
                ["Gender"] = "1",
                ["Street"] = "Portal Street",
                ["City"] = "Cairo",
                ["BuildingNumber"] = "9",
                ["Password"] = Password,
                ["ConfirmPassword"] = Password,
                ["__RequestVerificationToken"] = BrowserClient.ExtractToken(registerPage),
            });
        AssertRedirect(response, "/Account/Login");
    }

    private static async Task SeedBookingsAsync(GymTestAppFactory factory, string memberEmail)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<GymDbContext>();

        var memberId = await db.Members.AsNoTracking()
            .Where(member => member.Email == memberEmail)
            .Select(member => member.Id)
            .SingleAsync();

        var otherMember = new Member
        {
            Name = "Other Portal Member",
            Email = $"other.portal-{Guid.NewGuid():N}@example.com",
            Phone = $"011{Random.Shared.Next(10_000_000, 99_999_999)}",
            Photo = string.Empty,
            Gender = Gender.Female,
            DateOfBirth = new DateOnly(1997, 1, 1),
            Address = new Address { Street = "Other Street", City = "Cairo", BuildingNumber = 2 },
        };

        var category = new Category { CategoryName = "Portal Yoga" };
        var trainer = new Trainer
        {
            Name = "Portal Trainer",
            Email = $"portal.trainer-{Guid.NewGuid():N}@example.com",
            Phone = $"012{Random.Shared.Next(10_000_000, 99_999_999)}",
            Specialty = Specialty.Yoga,
            HiringDate = DateTime.UtcNow.AddYears(-2),
            Address = new Address { Street = "Coach Street", City = "Cairo", BuildingNumber = 3 },
        };

        db.Members.Add(otherMember);
        db.Categories.Add(category);
        db.Trainers.Add(trainer);
        await db.SaveChangesAsync();

        var ownSession = new Session
        {
            Description = "My portal session",
            Capacity = 10,
            StartDate = DateTime.UtcNow.AddDays(2),
            EndDate = DateTime.UtcNow.AddDays(2).AddHours(1),
            TrainerId = trainer.Id,
            CategoryId = category.Id,
        };
        var otherSession = new Session
        {
            Description = "Other member session",
            Capacity = 10,
            StartDate = DateTime.UtcNow.AddDays(3),
            EndDate = DateTime.UtcNow.AddDays(3).AddHours(1),
            TrainerId = trainer.Id,
            CategoryId = category.Id,
        };

        db.Sessions.AddRange(ownSession, otherSession);
        await db.SaveChangesAsync();

        db.Bookings.AddRange(
            new Booking
            {
                MemberId = memberId,
                SessionId = ownSession.Id,
                CreatedAt = DateTime.UtcNow,
            },
            new Booking
            {
                MemberId = otherMember.Id,
                SessionId = otherSession.Id,
                CreatedAt = DateTime.UtcNow,
            });
        await db.SaveChangesAsync();
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
