using GymManagement.BLL.ViewModels.BookingViewModels;
using GymManagement.BLL.ViewModels.SessionViewModels;
using GymManagement.DAL.Models;
using GymManagement.DAL.Models.Enums;

namespace GymManagement.Tests.Support;

internal static class TestData
{
    public static Trainer Trainer(Specialty specialty = Specialty.Cardio) => new()
    {
        Id = 10,
        Name = "Trainer One",
        Email = "trainer@example.com",
        Phone = "01012345678",
        Specialty = specialty,
        Address = new Address { BuildingNumber = 1, Street = "Main Street", City = "Cairo" },
    };

    public static Category Category(string name = "Cardio") => new()
    {
        Id = 20,
        CategoryName = name,
    };

    public static SessionListInfo SessionListInfo(
        DateTime? start = null,
        DateTime? end = null,
        int capacity = 10,
        int availableSlots = 10,
        int id = 30) => new()
    {
        Id = id,
        Description = "A valid training session",
        Capacity = capacity,
        StartDate = start ?? DateTime.UtcNow.AddDays(1),
        EndDate = end ?? DateTime.UtcNow.AddDays(1).AddHours(1),
        TrainerName = "Trainer One",
        CategoryName = "Cardio",
        AvailableSlots = availableSlots,
    };

    public static Session FutureSession(
        DateTime? start = null,
        DateTime? end = null,
        int capacity = 10,
        int id = 30) => new()
    {
        Id = id,
        Description = "A valid training session",
        Capacity = capacity,
        StartDate = start ?? DateTime.UtcNow.AddDays(1),
        EndDate = end ?? DateTime.UtcNow.AddDays(1).AddHours(1),
        TrainerId = 10,
        CategoryId = 20,
        Trainer = Trainer(),
        Category = Category(),
    };

    public static CreateSessionViewModel ValidCreateSession() => new()
    {
        Description = "A valid training session",
        Capacity = 10,
        StartDate = DateTime.UtcNow.AddDays(1),
        EndDate = DateTime.UtcNow.AddDays(1).AddHours(1),
        TrainerId = 10,
        CategoryId = 20,
    };

    public static UpdateSessionViewModel ValidUpdateSession() => new()
    {
        Description = "An updated training session",
        StartDate = DateTime.UtcNow.AddDays(2),
        EndDate = DateTime.UtcNow.AddDays(2).AddHours(1),
        TrainerId = 10,
    };

    public static CreateBookingViewModel CreateBooking(int memberId = 5, int sessionId = 30) => new()
    {
        MemberId = memberId,
        SessionId = sessionId,
    };

    public static Member Member() => new()
    {
        Id = 5,
        Name = "Member One",
        Email = "member@example.com",
        Phone = "01087654321",
        Gender = Gender.Male,
        DateOfBirth = new DateOnly(1995, 5, 20),
        Address = new Address { BuildingNumber = 12, Street = "Nile Street", City = "Cairo" },
    };
}
