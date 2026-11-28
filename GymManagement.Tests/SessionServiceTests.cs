using System.Linq.Expressions;
using GymManagement.BLL.Common;
using GymManagement.BLL.Services.Classes;
using GymManagement.DAL.Models;
using GymManagement.DAL.Models.Enums;
using GymManagement.DAL.Repositories.Interfaces;
using GymManagement.Tests.Support;
using Moq;
using Xunit;

namespace GymManagement.Tests;

public class SessionServiceTests
{
    private readonly UnitOfWorkMock _uow = new();
    private readonly SessionService _service;

    public SessionServiceTests()
    {
        _service = new SessionService(_uow.UnitOfWork.Object, TestMapper.Create());
    }

    [Fact]
    public async Task CreateSession_WhenEndDateIsNotAfterStart_ReturnsValidation()
    {
        var model = TestData.ValidCreateSession();
        model.EndDate = model.StartDate;

        var result = await _service.CreateSessionAsync(model);

        Assert.False(result.succes);
        Assert.Equal(ResultKind.ValidationFailed, result.kind);
        VerifyNoSave();
    }

    [Fact]
    public async Task CreateSession_WhenStartIsInPast_ReturnsValidation()
    {
        var model = TestData.ValidCreateSession();
        model.StartDate = DateTime.UtcNow.AddMinutes(-1);
        model.EndDate = DateTime.UtcNow.AddHours(1);

        var result = await _service.CreateSessionAsync(model);

        Assert.False(result.succes);
        Assert.Equal(ResultKind.ValidationFailed, result.kind);
        VerifyNoSave();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(26)]
    public async Task CreateSession_WhenCapacityIsOutOfRange_ReturnsValidation(int capacity)
    {
        var model = TestData.ValidCreateSession();
        model.Capacity = capacity;

        var result = await _service.CreateSessionAsync(model);

        Assert.False(result.succes);
        Assert.Equal(ResultKind.ValidationFailed, result.kind);
        VerifyNoSave();
    }

    [Fact]
    public async Task CreateSession_WhenTrainerDoesNotExist_ReturnsNotFound()
    {
        var model = TestData.ValidCreateSession();
        _uow.CategoryRepository
            .Setup(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(TestData.Category());

        var result = await _service.CreateSessionAsync(model);

        Assert.False(result.succes);
        Assert.Equal(ResultKind.NotFound, result.kind);
        Assert.Equal("Trainer Not Found", result.error);
        VerifyNoSave();
    }

    [Fact]
    public async Task CreateSession_WhenCategoryDoesNotExist_ReturnsNotFound()
    {
        var model = TestData.ValidCreateSession();
        _uow.TrainerRepository
            .Setup(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(TestData.Trainer());

        var result = await _service.CreateSessionAsync(model);

        Assert.False(result.succes);
        Assert.Equal(ResultKind.NotFound, result.kind);
        Assert.Equal("Category Not Found", result.error);
        VerifyNoSave();
    }

    [Fact]
    public async Task CreateSession_WhenTrainerSpecialtyDoesNotMatchCategory_ReturnsValidation()
    {
        var model = TestData.ValidCreateSession();
        SetupTrainerAndCategory(trainerSpecialty: Specialty.Yoga, categoryName: "Cardio");

        var result = await _service.CreateSessionAsync(model);

        Assert.False(result.succes);
        Assert.Equal(ResultKind.ValidationFailed, result.kind);
        VerifyNoSave();
    }

    [Fact]
    public async Task CreateSession_WhenValid_AddsSessionAndSaves()
    {
        var model = TestData.ValidCreateSession();
        SetupTrainerAndCategory();

        var result = await _service.CreateSessionAsync(model);

        Assert.True(result.succes);
        _uow.SessionRepositoryGeneric.Verify(
            r => r.Add(It.Is<Session>(s => s.Description == model.Description
                                       && s.Capacity == model.Capacity
                                       && s.TrainerId == model.TrainerId
                                       && s.CategoryId == model.CategoryId)),
            Times.Once);
        _uow.UnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateSession_WhenSessionDoesNotExist_ReturnsNotFound()
    {
        var result = await _service.UpdateSessionAsync(99, TestData.ValidUpdateSession());

        Assert.False(result.succes);
        Assert.Equal(ResultKind.NotFound, result.kind);
        VerifyNoSave();
    }

    [Fact]
    public async Task UpdateSession_WhenSessionAlreadyStarted_ReturnsConflict()
    {
        var session = TestData.FutureSession(start: DateTime.UtcNow.AddMinutes(-5), end: DateTime.UtcNow.AddHours(1));
        _uow.SessionRepository
            .Setup(r => r.GetByIdAsync(session.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);

        var result = await _service.UpdateSessionAsync(session.Id, TestData.ValidUpdateSession());

        Assert.False(result.succes);
        Assert.Equal(ResultKind.Conflict, result.kind);
        VerifyNoSave();
    }

    [Fact]
    public async Task UpdateSession_WhenSessionHasBookings_ReturnsConflict()
    {
        var session = TestData.FutureSession();
        _uow.SessionRepository
            .Setup(r => r.GetByIdAsync(session.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);
        _uow.SessionRepository
            .Setup(r => r.GetCountOfBookedSlotsAsync(session.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var result = await _service.UpdateSessionAsync(session.Id, TestData.ValidUpdateSession());

        Assert.False(result.succes);
        Assert.Equal(ResultKind.Conflict, result.kind);
        VerifyNoSave();
    }

    [Fact]
    public async Task UpdateSession_WhenEndDateIsNotAfterStart_ReturnsValidation()
    {
        var session = TestData.FutureSession();
        _uow.SessionRepository
            .Setup(r => r.GetByIdAsync(session.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);
        var model = TestData.ValidUpdateSession();
        model.EndDate = model.StartDate;

        var result = await _service.UpdateSessionAsync(session.Id, model);

        Assert.False(result.succes);
        Assert.Equal(ResultKind.ValidationFailed, result.kind);
        VerifyNoSave();
    }

    [Fact]
    public async Task UpdateSession_WhenTrainerDoesNotExist_ReturnsNotFound()
    {
        var session = TestData.FutureSession();
        _uow.SessionRepository
            .Setup(r => r.GetByIdAsync(session.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);
        _uow.CategoryRepository
            .Setup(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(TestData.Category());

        var result = await _service.UpdateSessionAsync(session.Id, TestData.ValidUpdateSession());

        Assert.False(result.succes);
        Assert.Equal(ResultKind.NotFound, result.kind);
        VerifyNoSave();
    }

    [Fact]
    public async Task UpdateSession_WhenValid_UpdatesSessionAndSaves()
    {
        var session = TestData.FutureSession();
        var model = TestData.ValidUpdateSession();
        _uow.SessionRepository
            .Setup(r => r.GetByIdAsync(session.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);
        SetupTrainerAndCategory();

        var result = await _service.UpdateSessionAsync(session.Id, model);

        Assert.True(result.succes);
        _uow.SessionRepository.Verify(
            r => r.Update(It.Is<Session>(s => s.Description == model.Description
                                          && s.StartDate == model.StartDate
                                          && s.EndDate == model.EndDate
                                          && s.UpdatedAt != null)),
            Times.Once);
        _uow.UnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RemoveSession_WhenSessionDoesNotExist_ReturnsNotFound()
    {
        var result = await _service.RemoveSessionAsync(99);

        Assert.False(result.succes);
        Assert.Equal(ResultKind.NotFound, result.kind);
        VerifyNoSave();
    }

    [Fact]
    public async Task RemoveSession_WhenSessionHasNotEnded_ReturnsConflict()
    {
        var session = TestData.FutureSession();
        _uow.SessionRepository
            .Setup(r => r.GetByIdAsync(session.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);

        var result = await _service.RemoveSessionAsync(session.Id);

        Assert.False(result.succes);
        Assert.Equal(ResultKind.Conflict, result.kind);
        VerifyNoSave();
    }

    [Fact]
    public async Task RemoveSession_WhenSessionHasBookings_ReturnsConflict()
    {
        var ended = TestData.FutureSession(start: DateTime.UtcNow.AddDays(-2), end: DateTime.UtcNow.AddDays(-1));
        _uow.SessionRepository
            .Setup(r => r.GetByIdAsync(ended.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ended);
        _uow.SessionRepository
            .Setup(r => r.GetCountOfBookedSlotsAsync(ended.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(2);

        var result = await _service.RemoveSessionAsync(ended.Id);

        Assert.False(result.succes);
        Assert.Equal(ResultKind.Conflict, result.kind);
        VerifyNoSave();
    }

    [Fact]
    public async Task RemoveSession_WhenEndedWithNoBookings_DeletesAndSaves()
    {
        var ended = TestData.FutureSession(start: DateTime.UtcNow.AddDays(-2), end: DateTime.UtcNow.AddDays(-1));
        _uow.SessionRepository
            .Setup(r => r.GetByIdAsync(ended.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ended);

        var result = await _service.RemoveSessionAsync(ended.Id);

        Assert.True(result.succes);
        _uow.SessionRepository.Verify(r => r.Delete(ended), Times.Once);
        _uow.UnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetSessionById_WhenMissing_ReturnsNotFound()
    {
        var result = await _service.GetSessionByIdAysnc(99, CancellationToken.None);

        Assert.False(result.success);
        Assert.Equal(ResultKind.NotFound, result.kind);
        Assert.Null(result.value);
    }

    [Fact]
    public async Task GetSessionById_CalculatesAvailableSlotsFromBookings()
    {
        var session = TestData.FutureSession(capacity: 10);
        _uow.SessionRepository
            .Setup(r => r.GetSessionByIdWithTrainerAndCategoryAsync(session.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);
        _uow.SessionRepository
            .Setup(r => r.GetCountOfBookedSlotsAsync(session.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(3);

        var result = await _service.GetSessionByIdAysnc(session.Id, CancellationToken.None);

        Assert.True(result.success);
        Assert.NotNull(result.value);
        Assert.Equal(7, result.value.AvailableSlots);
        Assert.Equal("Trainer One", result.value.TrainerName);
    }

    [Fact]
    public async Task GetAllSessions_UsesSingleProjection_AndDoesNotCountPerSession()
    {
        var older = TestData.SessionListInfo(
            id: 30,
            start: DateTime.UtcNow.AddDays(1),
            end: DateTime.UtcNow.AddDays(1).AddHours(1),
            capacity: 10,
            availableSlots: 7);
        var newer = TestData.SessionListInfo(
            id: 31,
            start: DateTime.UtcNow.AddDays(2),
            end: DateTime.UtcNow.AddDays(2).AddHours(1),
            capacity: 20,
            availableSlots: 15);
        _uow.SessionRepository
            .Setup(r => r.GetSessionsWithAvailableSlotsAsync(
                It.IsAny<Expression<Func<Session, bool>>?>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SessionListInfo> { newer, older });
        _uow.SessionRepository
            .Setup(r => r.CountAsync(
                It.IsAny<Expression<Func<Session, bool>>?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(20);

        var result = await _service.GetAllSessionsAync(CancellationToken.None, 2, 10);

        Assert.NotNull(result);
        Assert.Equal(new[] { 31, 30 }, result.Items.Select(s => s.Id));
        Assert.Equal(new[] { 15, 7 }, result.Items.Select(s => s.AvailableSlots));
        Assert.Equal("Trainer One", result.Items[0].TrainerName);
        Assert.Equal("Cardio", result.Items[0].CategoryName);
        Assert.Equal(2, result.Paging.Page);
        Assert.Equal(10, result.Paging.PageSize);
        Assert.Equal(20, result.Paging.TotalCount);
        Assert.Equal(2, result.Paging.TotalPages);
        _uow.SessionRepository.Verify(
            r => r.GetSessionsWithAvailableSlotsAsync(
                It.IsAny<Expression<Func<Session, bool>>?>(),
                2,
                10,
                It.IsAny<CancellationToken>()),
            Times.Once);
        _uow.SessionRepository.Verify(
            r => r.GetCountOfBookedSlotsAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    private void SetupTrainerAndCategory(
        Specialty trainerSpecialty = Specialty.Cardio,
        string categoryName = "Cardio")
    {
        _uow.TrainerRepository
            .Setup(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(TestData.Trainer(trainerSpecialty));
        _uow.CategoryRepository
            .Setup(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(TestData.Category(categoryName));
    }

    private void VerifyNoSave() =>
        _uow.UnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
}
