using GymManagement.BLL.ViewModels.MemberViewModels;
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
        private readonly GymDbContext _dbContext;
        private readonly UserManager<ApplicationUser> _userManager;

        public MemberPortalController(GymDbContext dbContext, UserManager<ApplicationUser> userManager)
        {
            _dbContext = dbContext;
            _userManager = userManager;
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
                                                  session.Description,
                                                  Category = category.CategoryName,
                                                  Trainer = trainer.Name,
                                                  session.StartDate,
                                                  session.EndDate,
                                                  booking.IsAttended,
                                              }).ToList(),
                              }).FirstOrDefaultAsync(ct);

            if (data is null)
            {
                var user = await _userManager.GetUserAsync(User);
                return View(new MemberPortalViewModel
                {
                    HasLinkedMember = false,
                    Name = $"{user?.FirstName} {user?.LastName}".Trim(),
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
                    Description = booking.Description,
                    Category = booking.Category,
                    Trainer = booking.Trainer,
                    StartDate = booking.StartDate.ToString("g"),
                    EndDate = booking.EndDate.ToString("g"),
                    IsAttended = booking.IsAttended,
                }).ToList(),
            };

            return View(model);
        }
    }
}
