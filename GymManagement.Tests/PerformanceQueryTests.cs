using System.Data.Common;
using GymManagement.DAL.Data.DbContexts;
using GymManagement.DAL.Models;
using GymManagement.DAL.Models.Enums;
using GymManagement.DAL.Repositories.Classes;
using GymManagement.Tests.Support;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Xunit;

namespace GymManagement.Tests;

/// <summary>
/// Regression coverage for the single-query replacements of the old N+1 list pages.
/// Requires GYM_TEST_CONNECTION_STRING; otherwise these tests are reported as skipped.
/// Each test gets a uniquely named database that is dropped again afterwards.
/// </summary>
public sealed class PerformanceQueryTests : IAsyncLifetime
{
    private readonly string? _connectionString =
        Environment.GetEnvironmentVariable("GYM_TEST_CONNECTION_STRING");
    private readonly string _databaseName = $"GymManagement_PerfTest_{Guid.NewGuid():N}";

    private readonly CommandCounter _counter = new();
    private GymDbContext? _db;

    public async Task InitializeAsync()
    {
        if (_connectionString is null)
        {
            return;
        }

        await TestDatabase.DropAsync(_connectionString, _databaseName);
        var options = new DbContextOptionsBuilder<GymDbContext>()
            .UseSqlServer(TestDatabase.ToTestConnectionString(_connectionString, _databaseName))
            .AddInterceptors(_counter)
            .Options;

        _db = new GymDbContext(options);
        await _db.Database.MigrateAsync();
        await SeedAsync(_db);
        _counter.Reset();
    }

    public async Task DisposeAsync()
    {
        if (_db is not null)
        {
            await _db.DisposeAsync();
        }

        if (_connectionString is not null)
        {
            await TestDatabase.DropAsync(_connectionString, _databaseName);
        }
    }

    [SqlServerFact]
    public async Task SessionListProjection_CountsBookingsInOneQuery()
    {
        var db = RequireDb();
        var repository = new SessionRepository(db);
        _counter.Reset();

        var sessions = (await repository.GetSessionsWithAvailableSlotsAsync()).ToList();

        Assert.Equal(2, sessions.Count);
        var full = sessions.Single(s => s.Description == "Full session");
        var empty = sessions.Single(s => s.Description == "Empty session");
        Assert.Equal(9, full.AvailableSlots);
        Assert.Equal(10, empty.AvailableSlots);
        Assert.All(sessions, s =>
        {
            Assert.Equal("Perf Trainer", s.TrainerName);
            Assert.Equal("Cardio", s.CategoryName);
        });
        Assert.Equal(1, _counter.Count);
    }

    [SqlServerFact]
    public async Task MemberListProjection_UsesSqlPagingAndDoesNotLoadPhotos()
    {
        var db = RequireDb();
        var repository = new MemberRepository(db);
        _counter.Reset();

        var members = (await repository.GetMembersForListAsync(1, 10)).ToList();

        Assert.Equal(2, members.Count);
        Assert.Equal(new[] { "Booked Member", "Unbooked Member" }, members.Select(m => m.Name));
        Assert.False(members.Single(m => m.Name == "Booked Member").HasPhoto);
        Assert.True(members.Single(m => m.Name == "Unbooked Member").HasPhoto);
        Assert.Equal(1, _counter.Count);
    }

    [SqlServerFact]
    public async Task MembershipListProjection_UsesSqlPagingAndLoadsNamesInOneQuery()
    {
        var db = RequireDb();
        var repository = new MembershipRepository(db);
        _counter.Reset();

        var memberships = (await repository.GetMembershipsForListAsync(m => m.EndDate > DateTime.UtcNow))
            .ToList();

        var membership = Assert.Single(memberships);
        Assert.Equal("Booked Member", membership.MemberName);
        Assert.Equal("Gold", membership.PlanName);
        Assert.Equal(new DateTime(2026, 1, 1), membership.StartDate);
        Assert.True(membership.EndDate > DateTime.UtcNow);
        Assert.Equal(1, _counter.Count);
    }

    [SqlServerFact]
    public async Task AvailableMembers_AntiJoinExcludesBookedMembersInOneQuery()
    {
        var db = RequireDb();
        var repository = new BookingRepository(db);
        var fullSessionId = await db.Sessions
            .Where(s => s.Description == "Full session")
            .Select(s => s.Id)
            .SingleAsync();
        _counter.Reset();

        var members = (await repository.GetAvailableMembersAsync(fullSessionId)).ToList();

        Assert.Equal(new[] { "Unbooked Member" }, members.Select(m => m.Name));
        Assert.Equal(1, _counter.Count);
    }

    private static async Task SeedAsync(GymDbContext db)
    {
        var category = new Category { CategoryName = "Cardio" };
        var trainer = new Trainer
        {
            Name = "Perf Trainer",
            Email = "perf.trainer@example.com",
            Phone = "01012345678",
            DateOfBirth = new DateOnly(1990, 1, 1),
            Gender = Gender.Male,
            HiringDate = DateTime.UtcNow.AddYears(-1),
            Specialty = Specialty.Cardio,
            Address = new Address { BuildingNumber = 1, Street = "Gym Street", City = "Cairo" },
        };
        var bookedMember = NewMember("Booked Member", "perf.booked@example.com", "01011111111");
        var unbookedMember = NewMember("Unbooked Member", "perf.unbooked@example.com", "01022222222");
        unbookedMember.Photo = "unbooked.png";
        var plan = new Plan
        {
            Name = "Gold",
            Description = "Gold plan",
            DurationDays = 30,
            Price = 100m,
            IsActive = true,
        };

        var full = NewSession("Full session", trainer, category);
        var empty = NewSession("Empty session", trainer, category);
        db.AddRange(category, trainer, bookedMember, unbookedMember, plan, full, empty);
        await db.SaveChangesAsync();

        db.Bookings.Add(new Booking { MemberId = bookedMember.Id, SessionId = full.Id });
        db.Memberships.Add(new Membership
        {
            MemberId = bookedMember.Id,
            PlanId = plan.Id,
            CreatedAt = new DateTime(2026, 1, 1),
            EndDate = DateTime.UtcNow.AddDays(30),
        });
        await db.SaveChangesAsync();
    }

    private static Member NewMember(string name, string email, string phone) => new()
    {
        Name = name,
        Email = email,
        Phone = phone,
        DateOfBirth = new DateOnly(1995, 5, 20),
        Gender = Gender.Female,
        Photo = string.Empty,
        Address = new Address { BuildingNumber = 12, Street = "Member Street", City = "Cairo" },
    };

    private static Session NewSession(string description, Trainer trainer, Category category) => new()
    {
        Description = description,
        Capacity = 10,
        StartDate = DateTime.UtcNow.AddDays(1),
        EndDate = DateTime.UtcNow.AddDays(1).AddHours(1),
        Trainer = trainer,
        Category = category,
    };

    private GymDbContext RequireDb() =>
        _db ?? throw new InvalidOperationException(
            "Set GYM_TEST_CONNECTION_STRING to run SQL Server performance regression tests.");

    private sealed class CommandCounter : DbCommandInterceptor
    {
        private int _count;

        public int Count => Volatile.Read(ref _count);

        public void Reset() => Interlocked.Exchange(ref _count, 0);

        public override InterceptionResult<DbDataReader> ReaderExecuting(
            DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result)
        {
            Interlocked.Increment(ref _count);
            return result;
        }

        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
            DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result,
            CancellationToken cancellationToken = default)
        {
            Interlocked.Increment(ref _count);
            return ValueTask.FromResult(result);
        }

        public override InterceptionResult<object> ScalarExecuting(
            DbCommand command, CommandEventData eventData, InterceptionResult<object> result)
        {
            Interlocked.Increment(ref _count);
            return result;
        }

        public override ValueTask<InterceptionResult<object>> ScalarExecutingAsync(
            DbCommand command, CommandEventData eventData, InterceptionResult<object> result,
            CancellationToken cancellationToken = default)
        {
            Interlocked.Increment(ref _count);
            return ValueTask.FromResult(result);
        }
    }
}
