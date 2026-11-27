using GymManagement.BLL.Services.Classes;
using GymManagement.BLL.ViewModels.TrainerViewModels;
using GymManagement.DAL.Models;
using GymManagement.DAL.Models.Enums;
using GymManagement.Tests.Support;
using Moq;
using Xunit;

namespace GymManagement.Tests;

public sealed class TrainerServiceTests
{
    private readonly UnitOfWorkMock _uow = new();
    private readonly TrainerService _service;

    public TrainerServiceTests()
    {
        _service = new TrainerService(_uow.UnitOfWork.Object, TestMapper.Create());
    }

    [Fact]
    public async Task CreateTrainer_SetsHiringDateToToday()
    {
        var before = DateTime.Now;
        var result = await _service.CreateTrainerAsync(CreateModel());
        var after = DateTime.Now;

        Assert.True(result.succes);
        _uow.TrainerRepository.Verify(
            repository => repository.Add(It.Is<Trainer>(trainer =>
                trainer.HiringDate >= before && trainer.HiringDate <= after)),
            Times.Once);
    }

    [Fact]
    public async Task UpdateTrainer_PreservesHiringDate()
    {
        var hiredOn = new DateTime(2020, 1, 15);
        var trainer = new Trainer
        {
            Id = 7,
            HiringDate = hiredOn,
            Address = new Address { BuildingNumber = 1, Street = "Old Street", City = "Cairo" },
        };
        _uow.TrainerRepository
            .Setup(repository => repository.GetByIdAsync(7, It.IsAny<CancellationToken>()))
            .ReturnsAsync(trainer);

        var result = await _service.UpdateTrainerDetailsAsync(7, UpdateModel());

        Assert.True(result.succes);
        Assert.Equal(hiredOn, trainer.HiringDate);
    }

    private static CreateTrainerViewModel CreateModel() => new()
    {
        Name = "New Trainer",
        Email = "new.trainer@example.com",
        Phone = "01012345678",
        DateOfBirth = new DateOnly(1990, 1, 1),
        Gender = Gender.Male,
        BuildingNumber = 1,
        Street = "Gym Street",
        City = "Cairo",
        Specialties = Specialty.Cardio,
    };

    private static TrainerToUpdateViewModel UpdateModel() => new()
    {
        Name = "Updated Trainer",
        Email = "updated.trainer@example.com",
        Phone = "01087654321",
        BuildingNumber = 2,
        Street = "Updated Street",
        City = "Alexandria",
        Specialties = Specialty.Yoga,
    };
}
