using GymManagement.DAL.Data.DbContexts;
using GymManagement.DAL.Models;
using GymManagement.DAL.Models.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GymManagement.DAL.Data.DataSeeding;

/// <summary>
/// Optional demo data for local/staging navigation. It is opt-in so tests and real
/// deployments keep their existing data, and every section is idempotent.
/// </summary>
public static class DemoDataSeeding
{
    private const string DemoMemberEmail = "demo.member@gymmanagement.local";
    private const string ActorEmail = "demo-seed@gymmanagement.local";

    public static async Task SeedAsync(
        GymDbContext dbContext,
        UserManager<ApplicationUser> userManager,
        ILogger logger,
        string? seedPassword,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(dbContext);
        ArgumentNullException.ThrowIfNull(userManager);
        ArgumentNullException.ThrowIfNull(logger);

        var now = DateTime.UtcNow;
        var demoUser = await EnsureDemoMemberUserAsync(userManager, logger, seedPassword, ct);
        await SeedTrainersAsync(dbContext, logger, now, ct);
        await SeedMembersAsync(dbContext, logger, demoUser?.Id, now, ct);
        await SeedHealthRecordsAsync(dbContext, logger, now, ct);
        await SeedMembershipsAsync(dbContext, logger, now, ct);
        await SeedSessionsAsync(dbContext, logger, now, ct);
        await SeedBookingsAsync(dbContext, logger, ct);
    }

    private static async Task<ApplicationUser?> EnsureDemoMemberUserAsync(
        UserManager<ApplicationUser> userManager,
        ILogger logger,
        string? seedPassword,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(seedPassword))
        {
            logger.LogWarning("Demo member user skipped because no seed password is configured.");
            return null;
        }

        var existing = await userManager.FindByEmailAsync(DemoMemberEmail);
        if (existing is not null)
        {
            await EnsureRoleAsync(userManager, existing, logger);
            return existing;
        }

        var user = new ApplicationUser
        {
            UserName = "DemoMember",
            Email = DemoMemberEmail,
            FirstName = "Demo",
            LastName = "Member",
            PhoneNumber = "01544445555",
            EmailConfirmed = true,
        };

        var created = await userManager.CreateAsync(user, seedPassword);
        if (!created.Succeeded)
        {
            logger.LogWarning(
                "Demo member user was not created: {Errors}",
                string.Join("; ", created.Errors.Select(error => error.Description)));
            return null;
        }

        await EnsureRoleAsync(userManager, user, logger);
        logger.LogInformation("Demo member user seeded: {Email}", DemoMemberEmail);
        return user;
    }

    private static async Task EnsureRoleAsync(
        UserManager<ApplicationUser> userManager,
        ApplicationUser user,
        ILogger logger)
    {
        if (await userManager.IsInRoleAsync(user, "Member"))
        {
            return;
        }

        var result = await userManager.AddToRoleAsync(user, "Member");
        if (!result.Succeeded)
        {
            logger.LogWarning(
                "Demo member role was not assigned: {Errors}",
                string.Join("; ", result.Errors.Select(error => error.Description)));
        }
    }

    private static async Task SeedTrainersAsync(
        GymDbContext db,
        ILogger logger,
        DateTime now,
        CancellationToken ct)
    {
        var specs = new (string Email, string Phone, string Name, Specialty Specialty, DateOnly Birth)[]
        {
            ("demo.trainer.cardio@gymmanagement.local", "01011112222", "Demo Cardio Coach", Specialty.Cardio, new DateOnly(1988, 4, 12)),
            ("demo.trainer.strength@gymmanagement.local", "01122223333", "Demo Strength Coach", Specialty.Strength, new DateOnly(1990, 7, 3)),
            ("demo.trainer.yoga@gymmanagement.local", "01233334444", "Demo Yoga Coach", Specialty.Yoga, new DateOnly(1992, 2, 21)),
        };

        var added = 0;
        foreach (var spec in specs)
        {
            if (await db.Trainers.AnyAsync(trainer => trainer.Email == spec.Email, ct))
            {
                continue;
            }

            db.Trainers.Add(new Trainer
            {
                Name = spec.Name,
                Email = spec.Email,
                Phone = spec.Phone,
                DateOfBirth = spec.Birth,
                Gender = Gender.Male,
                Specialty = spec.Specialty,
                HiringDate = now.AddMonths(-6),
                CreatedAt = now,
                Address = Address("Trainer Street", "Cairo", 10),
            });
            added++;
        }

        if (added > 0)
        {
            await db.SaveChangesAsync(ct);
            logger.LogInformation("Demo trainers seeded: {Count}", added);
        }
    }

    private static async Task SeedMembersAsync(
        GymDbContext db,
        ILogger logger,
        string? demoUserId,
        DateTime now,
        CancellationToken ct)
    {
        var specs = new (string Email, string Phone, string Name, Gender Gender, DateOnly Birth, bool LinkUser)[]
        {
            (DemoMemberEmail, "01544445555", "Demo Member One", Gender.Male, new DateOnly(1995, 5, 20), true),
            ("demo.member.two@gymmanagement.local", "01055556666", "Demo Member Two", Gender.Female, new DateOnly(1997, 9, 14), false),
            ("demo.member.three@gymmanagement.local", "01166667777", "Demo Member Three", Gender.Male, new DateOnly(1993, 1, 8), false),
        };

        var added = 0;
        foreach (var spec in specs)
        {
            if (await db.Members.AnyAsync(member => member.Email == spec.Email, ct))
            {
                continue;
            }

            db.Members.Add(new Member
            {
                Name = spec.Name,
                Email = spec.Email,
                Phone = spec.Phone,
                DateOfBirth = spec.Birth,
                Gender = spec.Gender,
                Photo = string.Empty,
                ApplicationUserId = spec.LinkUser ? demoUserId : null,
                CreatedAt = now,
                Address = Address("Member Street", "Giza", 20),
            });
            added++;
        }

        if (added > 0)
        {
            await db.SaveChangesAsync(ct);
            logger.LogInformation("Demo members seeded: {Count}", added);
        }
    }

    private static async Task SeedHealthRecordsAsync(
        GymDbContext db,
        ILogger logger,
        DateTime now,
        CancellationToken ct)
    {
        var members = await db.Members
            .Include(member => member.HealthRecord)
            .Where(member => member.Email!.StartsWith("demo.member"))
            .ToListAsync(ct);

        var records = new List<(decimal Height, decimal Weight, string BloodType, string Note)>
        {
            (175.5m, 70.5m, "O+", "Demo health record"),
            (165.0m, 58.0m, "A+", "Demo health record"),
            (180.0m, 82.5m, "B+", "Demo health record"),
        };

        var index = 0;
        foreach (var member in members)
        {
            if (member.HealthRecord is not null || index >= records.Count)
            {
                index++;
                continue;
            }

            var spec = records[index];
            var record = new HealthRecord
            {
                MemberId = member.Id,
                Height = spec.Height,
                Weight = spec.Weight,
                BloodType = spec.BloodType,
                Note = spec.Note,
                CreatedAt = now,
            };
            db.HealthRecords.Add(record);
            await db.SaveChangesAsync(ct);

            db.HealthRecordAudits.Add(new HealthRecordAudit
            {
                HealthRecordId = record.Id,
                MemberId = member.Id,
                ActorEmail = ActorEmail,
                Action = HealthRecordAudit.Created,
                Height = spec.Height,
                Weight = spec.Weight,
                BloodType = spec.BloodType,
                Note = spec.Note,
                CreatedAt = now,
            });
            index++;
        }

        if (index > 0)
        {
            await db.SaveChangesAsync(ct);
            logger.LogInformation("Demo health records seeded for {Count} demo members", index);
        }
    }

    private static async Task SeedMembershipsAsync(
        GymDbContext db,
        ILogger logger,
        DateTime now,
        CancellationToken ct)
    {
        var plans = await db.Plans.AsNoTracking().OrderBy(plan => plan.Id).ToListAsync(ct);
        var members = await db.Members
            .AsNoTracking()
            .Where(member => member.Email!.StartsWith("demo.member"))
            .OrderBy(member => member.Id)
            .ToListAsync(ct);
        if (plans.Count == 0 || members.Count == 0)
        {
            return;
        }

        var added = 0;
        for (var index = 0; index < members.Count; index++)
        {
            var plan = plans[index % plans.Count];
            var member = members[index];
            if (await db.Memberships.AnyAsync(
                    membership => membership.MemberId == member.Id && membership.PlanId == plan.Id, ct))
            {
                continue;
            }

            db.Memberships.Add(new Membership
            {
                MemberId = member.Id,
                PlanId = plan.Id,
                EndDate = index == 2 ? now.AddDays(-10) : now.AddDays(30 + (index * 15)),
                CreatedAt = now,
            });
            added++;
        }

        if (added > 0)
        {
            await db.SaveChangesAsync(ct);
            logger.LogInformation("Demo memberships seeded: {Count}", added);
        }
    }

    private static async Task SeedSessionsAsync(
        GymDbContext db,
        ILogger logger,
        DateTime now,
        CancellationToken ct)
    {
        var trainers = await db.Trainers.AsNoTracking().OrderBy(trainer => trainer.Id).ToListAsync(ct);
        var categories = await db.Categories.AsNoTracking().OrderBy(category => category.Id).ToListAsync(ct);
        if (trainers.Count == 0 || categories.Count == 0)
        {
            return;
        }

        var specs = new (string Description, int TrainerIndex, int CategoryIndex, double StartOffsetDays, double DurationHours)[]
        {
            ("Demo Morning Cardio", 0, 0, 2, 1),
            ("Demo Evening Strength", 1, 1, 4, 1.5),
            ("Demo Weekend Yoga", 2, 2, 6, 2),
        };

        var added = 0;
        foreach (var spec in specs)
        {
            if (await db.Sessions.AnyAsync(session => session.Description == spec.Description, ct))
            {
                continue;
            }

            var start = now.AddDays(spec.StartOffsetDays);
            db.Sessions.Add(new Session
            {
                Description = spec.Description,
                Capacity = 12,
                StartDate = start,
                EndDate = start.AddHours(spec.DurationHours),
                TrainerId = trainers[spec.TrainerIndex % trainers.Count].Id,
                CategoryId = categories[spec.CategoryIndex % categories.Count].Id,
                CreatedAt = now,
            });
            added++;
        }

        if (added > 0)
        {
            await db.SaveChangesAsync(ct);
            logger.LogInformation("Demo sessions seeded: {Count}", added);
        }
    }

    private static async Task SeedBookingsAsync(
        GymDbContext db,
        ILogger logger,
        CancellationToken ct)
    {
        var members = await db.Members
            .AsNoTracking()
            .Where(member => member.Email!.StartsWith("demo.member"))
            .OrderBy(member => member.Id)
            .ToListAsync(ct);
        var sessions = await db.Sessions
            .AsNoTracking()
            .Where(session => session.Description.StartsWith("Demo"))
            .OrderBy(session => session.StartDate)
            .ToListAsync(ct);
        if (members.Count < 2 || sessions.Count < 2)
        {
            return;
        }

        var pairs = new[]
        {
            (members[0].Id, sessions[0].Id),
            (members[1].Id, sessions[1].Id),
        };

        var added = 0;
        foreach (var (memberId, sessionId) in pairs)
        {
            if (await db.Bookings.AnyAsync(
                    booking => booking.MemberId == memberId && booking.SessionId == sessionId, ct))
            {
                continue;
            }

            db.Bookings.Add(new Booking
            {
                MemberId = memberId,
                SessionId = sessionId,
                IsAttended = false,
                CreatedAt = DateTime.UtcNow,
            });
            added++;
        }

        if (added > 0)
        {
            await db.SaveChangesAsync(ct);
            logger.LogInformation("Demo bookings seeded: {Count}", added);
        }
    }

    private static Address Address(string street, string city, int buildingNumber) => new()
    {
        Street = street,
        City = city,
        BuildingNumber = buildingNumber,
    };
}
