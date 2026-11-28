
using AutoMapper;
using GymManagement.BLL.Common;
using GymManagement.BLL.Services.Interfaces;
using GymManagement.BLL.ViewModels.BookingViewModels;
using GymManagement.BLL.ViewModels.MembershipViewModels;
using GymManagement.BLL.ViewModels.SessionViewModels;
using GymManagement.DAL.Models;
using GymManagement.DAL.Repositories.Classes;
using GymManagement.DAL.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace GymManagement.BLL.Services.Classes
{
    public class BookingService :IBookingService
    {
        private readonly IUnitOfWork unitOfWork;
        private readonly IMapper mapper;
        private readonly ILogger<BookingService> _logger;
        private readonly TimeProvider _timeProvider;

        public BookingService(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            ILogger<BookingService> logger,
            TimeProvider? timeProvider = null)
        {
            this.unitOfWork = unitOfWork;
            this.mapper = mapper;
            _logger = logger;
            _timeProvider = timeProvider ?? TimeProvider.System;
        }
        public async Task<Result> CancelBookingAsync(int memberId, int sessionId, CancellationToken ct = default)
        {
            var now = _timeProvider.GetUtcNow().UtcDateTime;
            var session = await unitOfWork.SessionRepository.GetByIdAsync(sessionId, ct);
            if (session is null) return Result.NotFound("Session not found.");

            if (session.StartDate <= now)
                return Result.Fail("Cannot cancel a booking for a session that has already started.");

            var booking = await unitOfWork.BookingRepository.FirstOrDefaultAsync(b => b.SessionId == sessionId && b.MemberId == memberId, tracking: true, ct: ct);
            if (booking is null) return Result.NotFound("Booking not found.");

            unitOfWork.BookingRepository.Delete(booking);
            var result = await unitOfWork.SaveChangesAsync(ct);
            return result > 0 ? Result.Ok() : Result.Fail("Booking Cancel Failed");
        }
        public async Task<Result> MarkAttendedAsync(int memberId, int sessionId, CancellationToken ct = default)
        {
            var booking = await unitOfWork.BookingRepository.FirstOrDefaultAsync(b => b.MemberId == memberId && b.SessionId == sessionId, tracking: true, ct: ct);
            if (booking is null) return Result.NotFound("Booking not found.");

            booking.IsAttended = true;
            booking.UpdatedAt = _timeProvider.GetUtcNow().UtcDateTime;
            unitOfWork.BookingRepository.Update(booking);

            var result = await unitOfWork.SaveChangesAsync(ct);
            return result > 0 ? Result.Ok() : Result.Fail("Failed to Mark As Attended");
        }
        public async Task<Result> CreateNewBookingAsync(CreateBookingViewModel model, CancellationToken ct = default)
        {
            var now = _timeProvider.GetUtcNow().UtcDateTime;

            // The checks and the insert share one transaction so a concurrent request
            // cannot observe a stale slot count and commit a conflicting booking.
            await using var transaction = await unitOfWork.BeginTransactionAsync(ct);

            try
            {
                var session = await unitOfWork.SessionRepository.GetByIdAsync(model.SessionId, ct);
                if (session is null) return Result.NotFound("Session not found.");

                if (session.StartDate <= now)
                    return Result.Fail("Cannot book a session that has already started.");

                var hasActiveMembership = await unitOfWork.MembershipRepository
                    .AnyAsync(m => m.MemberId == model.MemberId && m.EndDate > now, ct);
                if (!hasActiveMembership)
                    return Result.Fail("Member does not have an active membership.");

                var alreadyBooked = await unitOfWork.BookingRepository
                    .AnyAsync(b => b.SessionId == model.SessionId && b.MemberId == model.MemberId, ct);
                if (alreadyBooked)
                    return Result.Fail("Member is already booked for this session.");

                var booked = await unitOfWork.SessionRepository.GetCountOfBookedSlotsAsync(model.SessionId, ct);
                if (booked >= session.Capacity)
                    return Result.Fail("Session is full.");

                unitOfWork.BookingRepository.Add(new Booking
                {
                    MemberId = model.MemberId,
                    SessionId = model.SessionId,
                    IsAttended = false,
                    CreatedAt = now,
                });

                var result = await unitOfWork.SaveChangesAsync(ct);
                if (result <= 0)
                {
                    await transaction.RollbackAsync(ct);
                    return Result.Fail("Failed To Book Session");
                }

                await transaction.CommitAsync(ct);
                return Result.Ok();
            }
            catch (DbUpdateException ex)
            {
                await transaction.RollbackAsync(ct);

                // The unique (MemberId, SessionId) key is the final concurrency guard;
                // only that violation should produce the friendly duplicate message.
                if (DatabaseErrors.IsUniqueConstraintViolation(ex))
                {
                    return Result.Fail("Member is already booked for this session.");
                }

                _logger.LogError(
                    ex,
                    "Failed to create booking for member {MemberId} and session {SessionId}.",
                    model.MemberId,
                    model.SessionId);
                return Result.Fail("Booking could not be created. Please try again.");
            }
        }
        public async Task<PagedResult<SessionViewModel>> GetAllSessionsAsync(
            CancellationToken ct = default, int page = 1, int pageSize = 12)
        {
            page = Math.Max(page, 1);
            pageSize = Math.Clamp(pageSize, 1, 100);
            var now = _timeProvider.GetUtcNow().UtcDateTime;
            Expression<Func<Session, bool>> predicate = s => s.EndDate >= now;

            var sessions = await unitOfWork.SessionRepository.GetSessionsWithAvailableSlotsAsync(
                predicate, page, pageSize, ct);
            var totalCount = await unitOfWork.SessionRepository.CountAsync(predicate, ct);

            return new PagedResult<SessionViewModel>
            {
                Items = mapper.Map<List<SessionViewModel>>(sessions),
                Paging = new PagingInfo { Page = page, PageSize = pageSize, TotalCount = totalCount },
            };
        }
        public async Task<IEnumerable<MemberForSessionViewModel>> GetMembersForUpcomingBySessionIdAsync(
         int sessionId, CancellationToken ct = default)
        {
            var bookings = await unitOfWork.BookingRepository.GetBySessionIdAsync(sessionId, ct);
            return bookings.Select(b => new MemberForSessionViewModel
            {
                MemberId = b.MemberId,
                SessionId = sessionId,
                MemberName = b.Member.Name,
                BookingDate = b.CreatedAt.ToString("yyyy-MM-dd HH:mm"),
            }).ToList();
        }
        public async Task<IEnumerable<MemberForSessionViewModel>> GetMembersForOngoingBySessionIdAsync(
         int sessionId, CancellationToken ct = default)
        {
            var bookings = await unitOfWork.BookingRepository.GetBySessionIdAsync(sessionId, ct);
            return bookings.Select(b => new MemberForSessionViewModel
            {
                MemberId = b.MemberId,
                SessionId = sessionId,
                MemberName = b.Member.Name,
                BookingDate = b.CreatedAt.ToString("yyyy-MM-dd HH:mm"),
                IsAttended = b.IsAttended,
            }).ToList();
        }
        public async Task<IEnumerable<MemberSelectListViewModel>> GetMembersForDropDownAsync(int sessionId, CancellationToken ct = default)
        {
            var availableMembers = await unitOfWork.BookingRepository
                .GetAvailableMembersAsync(sessionId, ct);

            return mapper.Map<IEnumerable<MemberSelectListViewModel>>(availableMembers);
        }
    }
}
