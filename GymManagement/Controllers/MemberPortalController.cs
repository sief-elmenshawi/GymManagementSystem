using GymManagement.BLL.Service.Interfaces;
using GymManagement.BLL.Services.Interfaces;
using GymManagement.BLL.ViewModels.BookingViewModels;
using GymManagement.BLL.ViewModels.MemberViewModels;
using GymManagement.BLL.ViewModels.SessionViewModels;
using GymManagement.DAL.Data.DbContexts;
using GymManagement.DAL.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GymManagement.Controllers
{
    [Authorize(Roles = "Member")]
    public class MemberPortalController : Controller
    {
        private sealed record CurrentMember(ApplicationUser User, int MemberId);

        private readonly GymDbContext _dbContext;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IMemberService _memberService;
        private readonly IBookingService _bookingService;

        public MemberPortalController(
            GymDbContext dbContext,
            UserManager<ApplicationUser> userManager,
            IMemberService memberService,
            IBookingService bookingService)
        {
            _dbContext = dbContext;
            _userManager = userManager;
            _memberService = memberService;
            _bookingService = bookingService;
        }

        public async Task<IActionResult> Index(CancellationToken ct = default)
        {
            var userId = _userManager.GetUserId(User);

            var data = await (from member in _dbContext.Members.AsNoTracking()
                              where member.ApplicationUserId == userId
                              select new
                              {
                                  member.Id,
                                  member.Name,
                                  member.Email,
                                  member.Phone,
                                  member.DateOfBirth,
                                  member.Gender,
                                  Street = member.Address.Street,
                                  City = member.Address.City,
                                  BuildingNumber = member.Address.BuildingNumber,
                                  HasHealthRecord = _dbContext.HealthRecords
                                      .Any(record => record.MemberId == member.Id),
                                  Bookings = (from booking in _dbContext.Bookings.AsNoTracking()
                                              join session in _dbContext.Sessions.AsNoTracking()
                                                  on booking.SessionId equals session.Id
                                              join category in _dbContext.Categories.AsNoTracking()
                                                  on session.CategoryId equals category.Id
                                              join trainer in _dbContext.Trainers.AsNoTracking()
                                                  on session.TrainerId equals trainer.Id
                                              where booking.MemberId == member.Id
                                              orderby session.StartDate descending
                                              select new
                                              {
                                                  booking.SessionId,
                                                  session.Description,
                                                  Category = category.CategoryName,
                                                  Trainer = trainer.Name,
                                                  session.StartDate,
                                                  session.EndDate,
                                                  booking.IsAttended,
                                                  CanCancel = session.StartDate > DateTime.Now
                                                              && !booking.IsAttended,
                                              }).ToList(),
                              }).FirstOrDefaultAsync(ct);

            if (data is null)
            {
                var user = await _userManager.GetUserAsync(User);
                return View(new MemberPortalViewModel
                {
                    HasLinkedMember = false,
                    Name = FullName(user),
                    Email = user?.Email ?? string.Empty,
                    Notice = "No member profile is linked to this account yet. Please contact the gym administrator.",
                });
            }

            var model = new MemberPortalViewModel
            {
                MemberId = data.Id,
                HasLinkedMember = true,
                Name = data.Name,
                Email = data.Email,
                Phone = data.Phone,
                DateOfBirth = data.DateOfBirth.ToShortDateString(),
                Gender = data.Gender.ToString(),
                Address = $"{data.Street}, {data.City} - Building {data.BuildingNumber}",
                HasHealthRecord = data.HasHealthRecord,
                Bookings = data.Bookings.Select(booking => new MemberPortalBookingViewModel
                {
                    SessionId = booking.SessionId,
                    Description = booking.Description,
                    Category = booking.Category,
                    Trainer = booking.Trainer,
                    StartDate = booking.StartDate.ToString("g"),
                    EndDate = booking.EndDate.ToString("g"),
                    IsAttended = booking.IsAttended,
                    CanCancel = booking.CanCancel,
                }).ToList(),
            };

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> EditProfile(CancellationToken ct = default)
        {
            var current = await GetCurrentMemberAsync(ct);
            if (current is null) return Forbid();

            var model = await _memberService.GetMemberToUpdatAsync(current.MemberId, ct);
            if (model is null) return Forbid();

            model.Name = FullName(current.User);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditProfile(
            MemberToUpdateViewModel model,
            CancellationToken ct = default)
        {
            var current = await GetCurrentMemberAsync(ct);
            if (current is null) return Forbid();

            model.Name = FullName(current.User);

            if (!string.IsNullOrWhiteSpace(model.Email))
            {
                if (await _userManager.FindByEmailAsync(model.Email) is { } byEmail
                    && byEmail.Id != current.User.Id)
                {
                    ModelState.AddModelError(nameof(model.Email), "This email address is already in use.");
                }

                if (await _userManager.FindByNameAsync(model.Email) is { } byName
                    && byName.Id != current.User.Id)
                {
                    ModelState.AddModelError(nameof(model.Email), "This email address is already in use.");
                }
            }

            if (!ModelState.IsValid) return View(model);

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(ct);
            try
            {
                var memberResult = await _memberService.UpdateMemberDetailsAsync(
                    current.MemberId, model, ct);
                if (!memberResult.succes)
                {
                    ModelState.AddModelError(
                        string.Empty,
                        memberResult.error ?? "Profile could not be updated.");
                    await transaction.RollbackAsync(ct);
                    _dbContext.ChangeTracker.Clear();
                    return View(model);
                }

                current.User.Email = model.Email;
                current.User.UserName = model.Email;
                current.User.PhoneNumber = model.Phone;

                var identityResult = await _userManager.UpdateAsync(current.User);
                if (!identityResult.Succeeded)
                {
                    AddIdentityErrors(identityResult);
                    await transaction.RollbackAsync(ct);
                    _dbContext.ChangeTracker.Clear();
                    return View(model);
                }

                await transaction.CommitAsync(ct);
            }
            catch (DbUpdateException)
            {
                await transaction.RollbackAsync(ct);
                _dbContext.ChangeTracker.Clear();
                ModelState.AddModelError(
                    string.Empty,
                    "Profile could not be updated. Check that the email and phone number are unique.");
                return View(model);
            }

            TempData["SuccessMessage"] = "Your profile has been updated.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Sessions(int page = 1, CancellationToken ct = default)
        {
            var current = await GetCurrentMemberAsync(ct);
            if (current is null) return Forbid();

            return View(await _bookingService.GetAllSessionsAsync(ct, page));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Book(int sessionId, CancellationToken ct = default)
        {
            var current = await GetCurrentMemberAsync(ct);
            if (current is null) return Forbid();

            // SessionId comes from the form; the member id always comes from the signed-in user.
            var result = await _bookingService.CreateNewBookingAsync(
                new CreateBookingViewModel { MemberId = current.MemberId, SessionId = sessionId },
                ct);

            TempData[result.succes ? "SuccessMessage" : "ErrorMessage"] = result.succes
                ? "Your booking has been confirmed."
                : result.error ?? "The session could not be booked.";
            return RedirectToAction(nameof(Sessions));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int sessionId, CancellationToken ct = default)
        {
            var current = await GetCurrentMemberAsync(ct);
            if (current is null) return Forbid();

            var result = await _bookingService.CancelBookingAsync(current.MemberId, sessionId, ct);

            TempData[result.succes ? "SuccessMessage" : "ErrorMessage"] = result.succes
                ? "Your booking has been cancelled."
                : result.error ?? "The booking could not be cancelled.";
            return RedirectToAction(nameof(Index));
        }

        private async Task<CurrentMember?> GetCurrentMemberAsync(CancellationToken ct)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user is null) return null;

            var memberId = await _dbContext.Members.AsNoTracking()
                .Where(member => member.ApplicationUserId == user.Id)
                .Select(member => member.Id)
                .FirstOrDefaultAsync(ct);

            return memberId == 0 ? null : new CurrentMember(user, memberId);
        }

        private static string FullName(ApplicationUser? user) =>
            $"{user?.FirstName} {user?.LastName}".Trim();

        private void AddIdentityErrors(IdentityResult result)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
        }
    }
}
