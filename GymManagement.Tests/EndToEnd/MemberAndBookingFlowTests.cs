using System.Net;
using System.Text.RegularExpressions;
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
/// Real HTTP requests against a disposable SQL Server database: login, member CRUD and booking flow.
/// Requires GYM_TEST_CONNECTION_STRING; otherwise these tests are reported as skipped.
/// </summary>
public sealed class MemberAndBookingFlowTests
{
    private const string DatabaseName = "GymManagement_E2ETests";
    private const string SeedPassword = "Test@12345";
    private const string AdminEmail = "Serag@gmail.com";

    private readonly string? _connectionString =
        Environment.GetEnvironmentVariable("GYM_TEST_CONNECTION_STRING");

    [SqlServerFact]
    public async Task MemberLifecycle_CreateEditDelete_ViaHttp()
    {
        using var factory = await CreateFactoryAsync();
        var browser = CreateBrowser(factory);
        await LoginAsync(browser);

        var createPage = await browser.GetStringAsync("/Members/Create");
        var createResponse = await browser.PostMultipartAsync(
            "/Members/CreateMember",
            new Dictionary<string, string>
            {
                ["Name"] = "Integration Test Member",
                ["Email"] = "e2e.member@example.com",
                ["Phone"] = "01012345678",
                ["DateOfBirth"] = "1995-05-20",
                ["Gender"] = "1",
                ["BuildingNumber"] = "12",
                ["Street"] = "Test Street",
                ["City"] = "Cairo",
                ["HealthRecordViewModel.Height"] = "175.5",
                ["HealthRecordViewModel.Weight"] = "70.5",
                ["HealthRecordViewModel.BloodType"] = "O+",
                ["HealthRecordViewModel.Note"] = "created by end-to-end test",
                ["__RequestVerificationToken"] = BrowserClient.ExtractToken(createPage),
            },
            "PhotoFile",
            "member.png",
            TinyPng);

        Assert.Equal(HttpStatusCode.Redirect, createResponse.StatusCode);

        var index = await browser.GetStringAsync("/Members/Index");
        Assert.Contains("Integration Test Member", index, StringComparison.OrdinalIgnoreCase);

        var idMatch = Regex.Match(index, @"/Members/(?:MemberDetails|EditMember)/(\d+)");
        Assert.True(idMatch.Success, "Created member id was not found in the members list.");
        var memberId = int.Parse(idMatch.Groups[1].Value);

        var editPage = await browser.GetStringAsync($"/Members/EditMember/{memberId}");
        var editResponse = await browser.PostFormAsync(
            $"/Members/EditMember/{memberId}",
            new Dictionary<string, string>
            {
                ["Email"] = "e2e.updated@example.com",
                ["Phone"] = "01012345678",
                ["BuildingNumber"] = "15",
                ["Street"] = "Updated Street",
                ["City"] = "Alexandria",
                ["__RequestVerificationToken"] = BrowserClient.ExtractToken(editPage),
            });

        Assert.Equal(HttpStatusCode.Redirect, editResponse.StatusCode);
        var afterEdit = await browser.GetStringAsync("/Members/Index");
        Assert.Contains("Integration Test Member", afterEdit, StringComparison.OrdinalIgnoreCase);

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<GymDbContext>();
            var updatedMember = await db.Members.AsNoTracking().SingleAsync(m => m.Id == memberId);
            Assert.Equal("Alexandria", updatedMember.Address.City);
            Assert.Equal("Updated Street", updatedMember.Address.Street);
        }

        var deleteResponse = await browser.PostFormAsync(
            $"/Members/DeleteConfirmed/{memberId}",
            new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = BrowserClient.ExtractToken(editPage),
            });

        Assert.Equal(HttpStatusCode.Redirect, deleteResponse.StatusCode);
        var afterDelete = await browser.GetStringAsync("/Members/Index");
        Assert.DoesNotContain("Integration Test Member", afterDelete, StringComparison.OrdinalIgnoreCase);
    }

    [SqlServerFact]
    public async Task BookingFlow_CreateAndCancel_ViaHttp()
    {
        using var factory = await CreateFactoryAsync();
        var browser = CreateBrowser(factory);
        await LoginAsync(browser);

        int sessionId;
        int memberId;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<GymDbContext>();
            var plan = await db.Plans.FirstAsync();

            var category = new Category { CategoryName = "Yoga" };
            var trainer = new Trainer
            {
                Name = "E2E Trainer",
                Email = "e2e.trainer@example.com",
                Phone = "01055555555",
                Specialty = Specialty.Yoga,
                HiringDate = DateTime.Now.AddYears(-1),
                Address = new Address { BuildingNumber = 1, Street = "Gym Street", City = "Cairo" },
            };
            var member = new Member
            {
                Name = "Integration Booking Member",
                Email = "e2e.booking@example.com",
                Photo = "integration-booking-member.png",
                Phone = "01022222222",
                Gender = Gender.Female,
                DateOfBirth = new DateOnly(1998, 1, 1),
                Address = new Address { BuildingNumber = 7, Street = "Booking Street", City = "Cairo" },
                HealthRecord = new HealthRecord
                {
                    BloodType = "A+",
                    Height = 165,
                    Weight = 60,
                    Note = "created by end-to-end test",
                },
            };

            db.Categories.Add(category);
            db.Trainers.Add(trainer);
            db.Members.Add(member);
            await db.SaveChangesAsync();

            db.Memberships.Add(new Membership
            {
                MemberId = member.Id,
                PlanId = plan.Id,
                CreatedAt = DateTime.Now,
                EndDate = DateTime.Now.AddDays(30),
            });
            var session = new Session
            {
                Description = "E2E upcoming session",
                Capacity = 5,
                StartDate = DateTime.Now.AddDays(1),
                EndDate = DateTime.Now.AddDays(1).AddHours(1),
                TrainerId = trainer.Id,
                CategoryId = category.Id,
            };
            db.Sessions.Add(session);
            await db.SaveChangesAsync();

            sessionId = session.Id;
            memberId = member.Id;
        }

        var createPage = await browser.GetStringAsync($"/Booking/Create/{sessionId}");
        var createResponse = await browser.PostFormAsync(
            "/Booking/Create",
            new Dictionary<string, string>
            {
                ["SessionId"] = sessionId.ToString(),
                ["MemberId"] = memberId.ToString(),
                ["__RequestVerificationToken"] = BrowserClient.ExtractToken(createPage),
            });

        Assert.Equal(HttpStatusCode.Redirect, createResponse.StatusCode);
        var bookedMembers = await browser.GetStringAsync($"/Booking/GetMembersForUpcomingSession/{sessionId}");
        Assert.Contains("Integration Booking Member", bookedMembers, StringComparison.OrdinalIgnoreCase);

        var cancelResponse = await browser.PostFormAsync(
            "/Booking/Cancel",
            new Dictionary<string, string>
            {
                ["memberId"] = memberId.ToString(),
                ["sessionId"] = sessionId.ToString(),
                ["__RequestVerificationToken"] = BrowserClient.ExtractToken(createPage),
            });

        Assert.Equal(HttpStatusCode.Redirect, cancelResponse.StatusCode);
        var afterCancel = await browser.GetStringAsync($"/Booking/GetMembersForUpcomingSession/{sessionId}");
        Assert.DoesNotContain("Integration Booking Member", afterCancel, StringComparison.OrdinalIgnoreCase);
    }

    [SqlServerFact]
    public async Task PostWithoutAntiforgeryToken_IsRejected()
    {
        using var factory = await CreateFactoryAsync();
        var browser = CreateBrowser(factory);
        await LoginAsync(browser);

        var response = await browser.PostFormAsync(
            "/Members/CreateMember",
            new Dictionary<string, string>());

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private static BrowserClient CreateBrowser(GymTestAppFactory factory) =>
        new(factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
        }));

    private async Task<GymTestAppFactory> CreateFactoryAsync()
    {
        await TestDatabase.DropAsync(_connectionString!, DatabaseName);
        var factory = new GymTestAppFactory(_connectionString!, DatabaseName, SeedPassword);
        _ = factory.CreateClient(); // Force startup: migrations + seeding.
        return factory;
    }

    private async Task LoginAsync(BrowserClient browser)
    {
        var loginPage = await browser.GetStringAsync("/Account/Login");
        var response = await browser.PostFormAsync(
            "/Account/Login",
            new Dictionary<string, string>
            {
                ["Email"] = AdminEmail,
                ["Password"] = SeedPassword,
                ["RememberMe"] = "false",
                ["__RequestVerificationToken"] = BrowserClient.ExtractToken(loginPage),
            });

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
    }

    private static byte[] TinyPng => Convert.FromBase64String(
        "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+ip1sAAAAASUVORK5CYII=");
}
