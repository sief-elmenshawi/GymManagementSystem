using System.Net;
using GymManagement.DAL.Data.DbContexts;
using GymManagement.DAL.Models;
using GymManagement.DAL.Models.Enums;
using GymManagement.Infrastructure.Email;
using GymManagement.Tests.Support;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace GymManagement.Tests.EndToEnd;

/// <summary>
/// Real HTTP coverage for member self-service. Every request is issued as the signed-in
/// member, and the member id is never supplied by the test as a form field.
/// Requires GYM_TEST_CONNECTION_STRING; otherwise these tests are reported as skipped.
/// </summary>
public sealed class MemberSelfServiceFlowTests : IAsyncLifetime
{
    private const string Password = "Test@12345";
    private const string SuperAdminEmail = "Serag@gmail.com";

    private readonly string? _connectionString =
        Environment.GetEnvironmentVariable("GYM_TEST_CONNECTION_STRING");
    private readonly string _databaseName = $"GymManagement_E2E_SelfService_{Guid.NewGuid():N}";

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
    public async Task EditProfile_UpdatesOwnMemberAndIdentityContactDetails()
    {
        var factory = RequireFactory();
        var email = NewEmail("self.edit");
        var newEmail = NewEmail("self.updated");
        var newPhone = NewPhone();
        await RegisterAndConfirmAsync(factory, email);

        using var browser = CreateBrowser(factory);
        await LoginAsync(browser, email);

        var editPage = await browser.GetStringAsync("/Member/EditProfile");
        Assert.Contains(email, editPage, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Edit my profile", editPage, StringComparison.OrdinalIgnoreCase);

        using var response = await browser.PostFormAsync(
            "/Member/EditProfile",
            new Dictionary<string, string>
            {
                ["Email"] = newEmail,
                ["Phone"] = newPhone,
                ["BuildingNumber"] = "22",
                ["City"] = "Alexandria",
                ["Street"] = "Self Service Street",
                ["CurrentPassword"] = Password,
                ["__RequestVerificationToken"] = BrowserClient.ExtractToken(editPage),
            });
        AccountTestFlows.AssertRedirect(response, "/Member");

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<GymDbContext>();
            var member = await db.Members.AsNoTracking()
                .SingleAsync(member => member.Email == newEmail);
            Assert.Equal(newPhone, member.Phone);
            Assert.Equal(22, member.Address.BuildingNumber);
            Assert.Equal("Alexandria", member.Address.City);
            Assert.Equal("Self Service Street", member.Address.Street);

            var user = await db.Users.AsNoTracking()
                .SingleAsync(user => user.Id == member.ApplicationUserId);
            Assert.Equal(newEmail, user.Email);
            Assert.Equal(newEmail, user.UserName);
            Assert.Equal(newPhone, user.PhoneNumber);
            Assert.False(user.EmailConfirmed);
            Assert.False(await db.Users.AnyAsync(user => user.Email == email));
        }

        // The new address must be confirmed before the next sign-in.
        await AccountTestFlows.ConfirmEmailAsync(browser, newEmail);

        using var loginBrowser = CreateBrowser(factory);
        await LoginAsync(loginBrowser, newEmail);
    }

    [SqlServerFact]
    public async Task EditProfile_WrongCurrentPassword_LeavesProfileUnchanged()
    {
        var factory = RequireFactory();
        var email = NewEmail("self.wrong.password");
        var newEmail = NewEmail("self.wrong.updated");
        await RegisterAndConfirmAsync(factory, email);

        using var browser = CreateBrowser(factory);
        await LoginAsync(browser, email);

        var editPage = await browser.GetStringAsync("/Member/EditProfile");
        using var response = await browser.PostFormAsync(
            "/Member/EditProfile",
            new Dictionary<string, string>
            {
                ["Email"] = newEmail,
                ["Phone"] = NewPhone(),
                ["BuildingNumber"] = "99",
                ["City"] = "Cairo",
                ["Street"] = "Unchanged Street",
                ["CurrentPassword"] = "Wrong@12345",
                ["__RequestVerificationToken"] = BrowserClient.ExtractToken(editPage),
            });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("current password is incorrect", body, StringComparison.OrdinalIgnoreCase);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<GymDbContext>();
        Assert.True(await db.Members.AnyAsync(member => member.Email == email));
        Assert.False(await db.Members.AnyAsync(member => member.Email == newEmail));
        var user = await db.Users.AsNoTracking().SingleAsync(user => user.Email == email);
        Assert.True(user.EmailConfirmed);
    }

    [SqlServerFact]
    public async Task EditProfile_UnchangedEmail_KeepsEmailConfirmed()
    {
        var factory = RequireFactory();
        var email = NewEmail("self.same.email");
        var newPhone = NewPhone();
        await RegisterAndConfirmAsync(factory, email);
        factory.Services.GetRequiredService<DevelopmentMailbox>().Clear();

        using var browser = CreateBrowser(factory);
        await LoginAsync(browser, email);

        var editPage = await browser.GetStringAsync("/Member/EditProfile");
        using var response = await browser.PostFormAsync(
            "/Member/EditProfile",
            new Dictionary<string, string>
            {
                ["Email"] = email,
                ["Phone"] = newPhone,
                ["BuildingNumber"] = "12",
                ["City"] = "Giza",
                ["Street"] = "Same Email Street",
                ["CurrentPassword"] = Password,
                ["__RequestVerificationToken"] = BrowserClient.ExtractToken(editPage),
            });
        AccountTestFlows.AssertRedirect(response, "/Member");

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<GymDbContext>();
        var user = await db.Users.AsNoTracking().SingleAsync(user => user.Email == email);
        Assert.Equal(newPhone, user.PhoneNumber);
        Assert.True(user.EmailConfirmed);
        Assert.Empty(factory.Services.GetRequiredService<DevelopmentMailbox>().GetByEmail(email));
    }

    [SqlServerFact]
    public async Task SelfServicePages_WithoutLinkedProfile_AreForbidden()
    {
        var factory = RequireFactory();
        var email = NewEmail("unlinked.self");
        await CreateMemberRoleAccountAsync(factory, email);

        using var browser = CreateBrowser(factory);
        await LoginAsync(browser, email);

        foreach (var path in new[] { "/Member/EditProfile", "/Member/Sessions" })
        {
            using var response = await browser.GetAsync(path);
            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
            Assert.Contains("/Account/AccessDenied", response.Headers.Location?.OriginalString,
                StringComparison.OrdinalIgnoreCase);
        }

        var portal = await browser.GetStringAsync("/Member");
        Assert.Contains("No member profile is linked", portal, StringComparison.OrdinalIgnoreCase);
    }

    [SqlServerFact]
    public async Task BookAndCancel_UseSignedInMemberOnly()
    {
        var factory = RequireFactory();
        var email = NewEmail("self.book");
        await RegisterAndConfirmAsync(factory, email);
        var (sessionId, memberId) = await SeedSessionAsync(
            factory, email, "Self Yoga", DateTime.Now.AddDays(2), activeMembership: true);

        using var browser = CreateBrowser(factory);
        await LoginAsync(browser, email);

        var sessionsPage = await browser.GetStringAsync("/Member/Sessions");
        Assert.Contains("Self Yoga", sessionsPage, StringComparison.OrdinalIgnoreCase);

        using var bookResponse = await browser.PostFormAsync(
            "/Member/Book",
            new Dictionary<string, string>
            {
                ["sessionId"] = sessionId.ToString(),
                ["__RequestVerificationToken"] = BrowserClient.ExtractToken(sessionsPage),
            });
        AccountTestFlows.AssertRedirect(bookResponse, "/Member/Sessions");

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<GymDbContext>();
            var booking = await db.Bookings.AsNoTracking()
                .SingleAsync(booking => booking.SessionId == sessionId);
            Assert.Equal(memberId, booking.MemberId);
            Assert.False(booking.IsAttended);
        }

        var portal = await browser.GetStringAsync("/Member");
        Assert.Contains("Self Yoga", portal, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Cancel", portal, StringComparison.OrdinalIgnoreCase);

        using var cancelResponse = await browser.PostFormAsync(
            "/Member/Cancel",
            new Dictionary<string, string>
            {
                ["sessionId"] = sessionId.ToString(),
                ["__RequestVerificationToken"] = BrowserClient.ExtractToken(portal),
            });
        AccountTestFlows.AssertRedirect(cancelResponse, "/Member");

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<GymDbContext>();
            Assert.False(await db.Bookings.AnyAsync(booking => booking.SessionId == sessionId));
        }
    }

    [SqlServerFact]
    public async Task Book_RejectsStartedSessionAndMissingActiveMembership()
    {
        var factory = RequireFactory();
        var email = NewEmail("self.rules");
        await RegisterAndConfirmAsync(factory, email);
        var (startedSessionId, _) = await SeedSessionAsync(
            factory, email, "Started Yoga", DateTime.Now.AddMinutes(-30), activeMembership: true);
        var (futureSessionId, memberId) = await SeedSessionAsync(
            factory, email, "Future Yoga", DateTime.Now.AddDays(3), activeMembership: false);

        using var browser = CreateBrowser(factory);
        await LoginAsync(browser, email);

        var sessionsPage = await browser.GetStringAsync("/Member/Sessions");
        using var startedResponse = await browser.PostFormAsync(
            "/Member/Book",
            new Dictionary<string, string>
            {
                ["sessionId"] = startedSessionId.ToString(),
                ["__RequestVerificationToken"] = BrowserClient.ExtractToken(sessionsPage),
            });
        AccountTestFlows.AssertRedirect(startedResponse, "/Member/Sessions");

        sessionsPage = await browser.GetStringAsync("/Member/Sessions");
        Assert.Contains("already started", sessionsPage, StringComparison.OrdinalIgnoreCase);

        await ExpireMembershipsAsync(factory, memberId);

        sessionsPage = await browser.GetStringAsync("/Member/Sessions");
        using var noMembershipResponse = await browser.PostFormAsync(
            "/Member/Book",
            new Dictionary<string, string>
            {
                ["sessionId"] = futureSessionId.ToString(),
                ["__RequestVerificationToken"] = BrowserClient.ExtractToken(sessionsPage),
            });
        AccountTestFlows.AssertRedirect(noMembershipResponse, "/Member/Sessions");

        sessionsPage = await browser.GetStringAsync("/Member/Sessions");
        Assert.Contains("active membership", sessionsPage, StringComparison.OrdinalIgnoreCase);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<GymDbContext>();
        Assert.False(await db.Bookings.AnyAsync(booking =>
            booking.MemberId == memberId &&
            (booking.SessionId == startedSessionId || booking.SessionId == futureSessionId)));
    }

    private static async Task RegisterAndConfirmAsync(GymTestAppFactory factory, string email)
    {
        using var browser = CreateBrowser(factory);
        await AccountTestFlows.RegisterAsync(browser, email, NewPhone(), Password, "Self", "Service");
        await AccountTestFlows.ConfirmEmailAsync(browser, email);
    }

    private static async Task CreateMemberRoleAccountAsync(GymTestAppFactory factory, string email)
    {
        using var adminBrowser = CreateBrowser(factory);
        await AccountTestFlows.LoginAsync(adminBrowser, SuperAdminEmail, Password);

        var createPage = await adminBrowser.GetStringAsync("/Users/Create");
        using var response = await adminBrowser.PostFormAsync(
            "/Users/Create",
            new Dictionary<string, string>
            {
                ["FirstName"] = "Unlinked",
                ["LastName"] = "Self",
                ["UserName"] = email,
                ["Email"] = email,
                ["PhoneNumber"] = NewPhone(),
                ["Role"] = "Member",
                ["Password"] = Password,
                ["__RequestVerificationToken"] = BrowserClient.ExtractToken(createPage),
            });
        AccountTestFlows.AssertRedirect(response, "/Users");
    }

    private static async Task<(int SessionId, int MemberId)> SeedSessionAsync(
        GymTestAppFactory factory,
        string memberEmail,
        string description,
        DateTime startDate,
        bool activeMembership)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<GymDbContext>();

        var memberId = await db.Members.AsNoTracking()
            .Where(member => member.Email == memberEmail)
            .Select(member => member.Id)
            .SingleAsync();

        var plan = await db.Plans.AsNoTracking().FirstOrDefaultAsync();
        if (plan is null)
        {
            plan = new Plan
            {
                Name = "Self Service Plan",
                Description = "Test plan",
                DurationDays = 30,
                Price = 100m,
                IsActive = true,
                CreatedAt = DateTime.Now,
            };
            db.Plans.Add(plan);
            await db.SaveChangesAsync();
        }

        if (activeMembership)
        {
            db.Memberships.Add(new Membership
            {
                MemberId = memberId,
                PlanId = plan.Id,
                EndDate = DateTime.Now.AddDays(30),
                CreatedAt = DateTime.Now,
            });
        }

        var category = new Category
        {
            CategoryName = "SelfYoga",
            CreatedAt = DateTime.Now,
        };
        var trainer = new Trainer
        {
            Name = "Self Service Coach",
            Email = NewEmail("coach"),
            Phone = NewPhone(),
            Specialty = Specialty.Yoga,
            HiringDate = DateTime.Now.AddYears(-1),
            CreatedAt = DateTime.Now,
            Address = new Address { Street = "Coach Street", City = "Cairo", BuildingNumber = 4 },
        };

        db.Categories.Add(category);
        db.Trainers.Add(trainer);
        await db.SaveChangesAsync();

        var session = new Session
        {
            Description = description,
            Capacity = 10,
            StartDate = startDate,
            EndDate = startDate.AddHours(1),
            CategoryId = category.Id,
            TrainerId = trainer.Id,
            CreatedAt = DateTime.Now,
        };
        db.Sessions.Add(session);
        await db.SaveChangesAsync();

        return (session.Id, memberId);
    }

    private static async Task ExpireMembershipsAsync(GymTestAppFactory factory, int memberId)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<GymDbContext>();
        var memberships = await db.Memberships
            .Where(membership => membership.MemberId == memberId)
            .ToListAsync();

        foreach (var membership in memberships)
        {
            membership.EndDate = DateTime.Now.AddDays(-1);
        }

        await db.SaveChangesAsync();
    }

    private static async Task LoginAsync(BrowserClient browser, string email) =>
        await AccountTestFlows.LoginAsync(browser, email, Password);

    private static BrowserClient CreateBrowser(GymTestAppFactory factory) =>
        new(factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
        }));

    private GymTestAppFactory RequireFactory() =>
        _factory ?? throw new InvalidOperationException(
            "Set GYM_TEST_CONNECTION_STRING to run SQL Server end-to-end tests.");

    private static string NewEmail(string prefix) =>
        $"{prefix}-{Guid.NewGuid():N}@example.com";

    private static string NewPhone() =>
        $"010{Random.Shared.Next(10_000_000, 99_999_999)}";
}
