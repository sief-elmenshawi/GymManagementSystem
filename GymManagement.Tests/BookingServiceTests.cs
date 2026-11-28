using System.Linq.Expressions;
using GymManagement.BLL.Common;
using GymManagement.BLL.Services.Classes;
using GymManagement.DAL.Models;
using GymManagement.Tests.Support;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace GymManagement.Tests;

public class BookingServiceTests
{
    private readonly UnitOfWorkMock _uow = new();
    private readonly BookingService _service;

    public BookingServiceTests()
    {
        _service = new BookingService(
            _uow.UnitOfWork.Object,
            TestMapper.Create(),
            NullLogger<BookingService>.Instance);
    }

    [Fact]
    public async Task CreateBooking_WhenSessionDoesNotExist_ReturnsNotFound()
    {
        var result = await _service.CreateNewBookingAsync(TestData.CreateBooking());

        Assert.False(result.succes);
        Assert.Equal(ResultKind.NotFound, result.kind);
        VerifyNoSave();
    }

    [Fact]
    public async Task CreateBooking_WhenSessionAlreadyStarted_ReturnsConflict()
    {
        SetupSession(TestData.FutureSession(start: DateTime.UtcNow.AddMinutes(-10), end: DateTime.UtcNow.AddHours(1)));

        var result = await _service.CreateNewBookingAsync(TestData.CreateBooking());

        Assert.False(result.succes);
        Assert.Equal(ResultKind.Conflict, result.kind);
        VerifyNoSave();
    }

    [Fact]
    public async Task CreateBooking_WhenMemberHasNoActiveMembership_ReturnsConflict()
    {
        SetupSession(TestData.FutureSession());
        SetupMembership(active: false);

        var result = await _service.CreateNewBookingAsync(TestData.CreateBooking());

        Assert.False(result.succes);
        Assert.Equal(ResultKind.Conflict, result.kind);
        Assert.Equal("Member does not have an active membership.", result.error);
        VerifyNoSave();
    }

    [Fact]
    public async Task CreateBooking_WhenMemberAlreadyBooked_ReturnsConflict()
    {
        SetupSession(TestData.FutureSession());
        SetupMembership(active: true);
        SetupAlreadyBooked(true);

        var result = await _service.CreateNewBookingAsync(TestData.CreateBooking());

        Assert.False(result.succes);
        Assert.Equal(ResultKind.Conflict, result.kind);
        Assert.Equal("Member is already booked for this session.", result.error);
        VerifyNoSave();
    }

    [Fact]
    public async Task CreateBooking_WhenSessionIsFull_ReturnsConflict()
    {
        var session = TestData.FutureSession(capacity: 2);
        SetupSession(session);
        SetupMembership(active: true);
        SetupAlreadyBooked(false);
        _uow.SessionRepository
            .Setup(r => r.GetCountOfBookedSlotsAsync(session.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(2);

        var result = await _service.CreateNewBookingAsync(TestData.CreateBooking());

        Assert.False(result.succes);
        Assert.Equal(ResultKind.Conflict, result.kind);
        Assert.Equal("Session is full.", result.error);
        VerifyNoSave();
    }

    [Fact]
    public async Task CreateBooking_WhenValid_AddsBookingAndSaves()
    {
        var session = TestData.FutureSession(capacity: 10);
        var model = TestData.CreateBooking(memberId: 5, sessionId: session.Id);
        SetupSession(session);
        SetupMembership(active: true);
        SetupAlreadyBooked(false);
        _uow.SessionRepository
            .Setup(r => r.GetCountOfBookedSlotsAsync(session.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(4);

        var result = await _service.CreateNewBookingAsync(model);

        Assert.True(result.succes);
        _uow.BookingRepository.Verify(
            r => r.Add(It.Is<Booking>(b => b.MemberId == model.MemberId
                                       && b.SessionId == model.SessionId
                                       && !b.IsAttended)),
            Times.Once);
        _uow.UnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateBooking_WhenUniqueKeyRaceOccurs_ReturnsFriendlyConflict()
    {
        var session = TestData.FutureSession(capacity: 10);
        var model = TestData.CreateBooking(memberId: 5, sessionId: session.Id);
        SetupSession(session);
        SetupMembership(active: true);
        SetupAlreadyBooked(false);
        _uow.SessionRepository
            .Setup(r => r.GetCountOfBookedSlotsAsync(session.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(4);
        _uow.UnitOfWork
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DbUpdateException("Duplicate key", new Exception()));

        var result = await _service.CreateNewBookingAsync(model);

        Assert.False(result.succes);
        Assert.Equal(ResultKind.Conflict, result.kind);
        Assert.Equal("Member is already booked for this session.", result.error);
        _uow.UnitOfWork.Verify(
            u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()),
            Times.Once);
        _uow.UnitOfWork.Verify(
            u => u.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task CreateBooking_WhenDatabaseFails_ReturnsGenericError()
    {
        var session = TestData.FutureSession(capacity: 10);
        var model = TestData.CreateBooking();
        SetupSession(session);
        SetupMembership(active: true);
        SetupAlreadyBooked(false);
        _uow.SessionRepository
            .Setup(r => r.GetCountOfBookedSlotsAsync(session.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(4);
        _uow.UnitOfWork
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DbUpdateException("Connection reset by peer", new Exception()));

        var result = await _service.CreateNewBookingAsync(model);

        Assert.False(result.succes);
        Assert.Equal("Booking could not be created. Please try again.", result.error);
    }

    [Fact]
    public async Task CancelBooking_WhenSessionDoesNotExist_ReturnsNotFound()
    {
        var result = await _service.CancelBookingAsync(5, 30);

        Assert.False(result.succes);
        Assert.Equal(ResultKind.NotFound, result.kind);
        VerifyNoSave();
    }

    [Fact]
    public async Task CancelBooking_WhenSessionAlreadyStarted_ReturnsConflict()
    {
        SetupSession(TestData.FutureSession(start: DateTime.UtcNow.AddMinutes(-10), end: DateTime.UtcNow.AddHours(1)));

        var result = await _service.CancelBookingAsync(5, 30);

        Assert.False(result.succes);
        Assert.Equal(ResultKind.Conflict, result.kind);
        VerifyNoSave();
    }

    [Fact]
    public async Task CancelBooking_WhenBookingDoesNotExist_ReturnsNotFound()
    {
        SetupSession(TestData.FutureSession());

        var result = await _service.CancelBookingAsync(5, 30);

        Assert.False(result.succes);
        Assert.Equal(ResultKind.NotFound, result.kind);
        VerifyNoSave();
    }

    [Fact]
    public async Task CancelBooking_WhenBookingExists_DeletesAndSaves()
    {
        var session = TestData.FutureSession();
        var booking = new Booking { Id = 1, MemberId = 5, SessionId = session.Id };
        SetupSession(session);
        SetupBooking(booking);

        var result = await _service.CancelBookingAsync(5, session.Id);

        Assert.True(result.succes);
        _uow.BookingRepository.Verify(r => r.Delete(booking), Times.Once);
        _uow.UnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task MarkAttended_WhenBookingDoesNotExist_ReturnsNotFound()
    {
        var result = await _service.MarkAttendedAsync(5, 30);

        Assert.False(result.succes);
        Assert.Equal(ResultKind.NotFound, result.kind);
        VerifyNoSave();
    }

    [Fact]
    public async Task MarkAttended_WhenBookingExists_SetsAttendanceAndSaves()
    {
        var booking = new Booking { Id = 1, MemberId = 5, SessionId = 30 };
        SetupBooking(booking);

        var result = await _service.MarkAttendedAsync(5, 30);

        Assert.True(result.succes);
        Assert.True(booking.IsAttended);
        Assert.NotNull(booking.UpdatedAt);
        _uow.BookingRepository.Verify(r => r.Update(booking), Times.Once);
        _uow.UnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetMembersForOngoingSession_IncludesAttendanceAndMemberName()
    {
        var member = TestData.Member();
        var bookings = new List<Booking>
        {
            new() { Id = 1, MemberId = member.Id, SessionId = 30, IsAttended = true, Member = member },
            new() { Id = 2, MemberId = 6, SessionId = 30, IsAttended = false, Member = new Member { Name = "Member Two" } },
        };
        _uow.BookingRepository
            .Setup(r => r.GetBySessionIdAsync(30, It.IsAny<CancellationToken>()))
            .ReturnsAsync(bookings);

        var result = (await _service.GetMembersForOngoingBySessionIdAsync(30)).ToList();

        Assert.Equal(2, result.Count);
        Assert.Equal("Member One", result[0].MemberName);
        Assert.True(result[0].IsAttended);
        Assert.False(result[1].IsAttended);
    }

    [Fact]
    public async Task GetAllSessions_UsesSingleProjection_AndDoesNotCountPerSession()
    {
        var session = TestData.SessionListInfo(capacity: 10, availableSlots: 4);
        _uow.SessionRepository
            .Setup(r => r.GetSessionsWithAvailableSlotsAsync(
                It.IsAny<Expression<Func<Session, bool>>?>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SessionListInfo> { session });
        _uow.SessionRepository
            .Setup(r => r.CountAsync(
                It.IsAny<Expression<Func<Session, bool>>?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var result = await _service.GetAllSessionsAsync(CancellationToken.None, 3, 5);

        Assert.Single(result.Items);
        Assert.Equal(4, result.Items[0].AvailableSlots);
        Assert.Equal("Trainer One", result.Items[0].TrainerName);
        Assert.Equal(3, result.Paging.Page);
        Assert.Equal(5, result.Paging.PageSize);
        Assert.Equal(1, result.Paging.TotalCount);
        _uow.SessionRepository.Verify(
            r => r.GetSessionsWithAvailableSlotsAsync(
                It.IsAny<Expression<Func<Session, bool>>?>(),
                3,
                5,
                It.IsAny<CancellationToken>()),
            Times.Once);
        _uow.SessionRepository.Verify(
            r => r.GetCountOfBookedSlotsAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task GetMembersForDropDown_UsesAntiJoin_InsteadOfLoadingAllBookings()
    {
        _uow.BookingRepository
            .Setup(r => r.GetAvailableMembersAsync(30, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Member>
            {
                new() { Id = 5, Name = "Available One" },
                new() { Id = 6, Name = "Available Two" },
            });

        var result = (await _service.GetMembersForDropDownAsync(30, CancellationToken.None)).ToList();

        Assert.Equal(new[] { "Available One", "Available Two" }, result.Select(m => m.Name));
        _uow.BookingRepository.Verify(
            r => r.GetAvailableMembersAsync(30, It.IsAny<CancellationToken>()),
            Times.Once);
        _uow.BookingRepository.Verify(
            r => r.GetAllAsync(
                It.IsAny<Expression<Func<Booking, bool>>?>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
        _uow.MemberRepository.Verify(
            r => r.GetAllAsync(
                It.IsAny<Expression<Func<Member, bool>>?>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    private void SetupSession(Session session) =>
        _uow.SessionRepository
            .Setup(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);

    private void SetupMembership(bool active) =>
        _uow.MembershipRepository
            .Setup(r => r.AnyAsync(
                It.IsAny<Expression<Func<Membership, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(active);

    private void SetupAlreadyBooked(bool alreadyBooked) =>
        _uow.BookingRepository
            .Setup(r => r.AnyAsync(
                It.IsAny<Expression<Func<Booking, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(alreadyBooked);

    private void SetupBooking(Booking? booking) =>
        _uow.BookingRepository
            .Setup(r => r.FirstOrDefaultAsync(
                It.IsAny<Expression<Func<Booking, bool>>>(),
                true,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(booking);

    private void VerifyNoSave() =>
        _uow.UnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
}
