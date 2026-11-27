using GymManagement.BLL.ViewModels.ApplicationViewModels;
using GymManagement.DAL.Data.DbContexts;
using GymManagement.DAL.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GymManagement.Controllers
{
    [Authorize(Roles = "SuperAdmin")]
    public class UsersController : Controller
    {
        private const string SuperAdminRole = "SuperAdmin";

        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly GymDbContext _dbContext;
        private readonly ILogger<UsersController> _logger;

        public UsersController(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            GymDbContext dbContext,
            ILogger<UsersController> logger)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task<IActionResult> Index(CancellationToken ct = default)
        {
            var users = await (from user in _dbContext.Users.AsNoTracking()
                               orderby user.UserName
                               select new UserListViewModel
                               {
                                   Id = user.Id,
                                   UserName = user.UserName ?? string.Empty,
                                   Email = user.Email ?? string.Empty,
                                   FirstName = user.FirstName,
                                   LastName = user.LastName,
                                   PhoneNumber = user.PhoneNumber,
                                   Roles = (from userRole in _dbContext.UserRoles
                                            join role in _dbContext.Roles on userRole.RoleId equals role.Id
                                            where userRole.UserId == user.Id
                                            select role.Name!).ToList(),
                               }).ToListAsync(ct);

            return View(users);
        }

        public async Task<IActionResult> Create(CancellationToken ct = default)
        {
            var model = new CreateUserViewModel
            {
                AvailableRoles = await GetRolesAsync(ct),
            };
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Create(CreateUserViewModel model, CancellationToken ct = default)
        {
            if (!await _roleManager.RoleExistsAsync(model.Role))
            {
                ModelState.AddModelError(nameof(model.Role), "Selected role does not exist.");
            }

            if (await _userManager.FindByEmailAsync(model.Email) is not null)
            {
                ModelState.AddModelError(nameof(model.Email), "A user with this email already exists.");
            }

            if (await _userManager.FindByNameAsync(model.UserName) is not null)
            {
                ModelState.AddModelError(nameof(model.UserName), "This user name is already taken.");
            }

            if (!ModelState.IsValid)
            {
                model.AvailableRoles = await GetRolesAsync(ct);
                return View(model);
            }

            var user = new ApplicationUser
            {
                FirstName = model.FirstName,
                LastName = model.LastName,
                UserName = model.UserName,
                Email = model.Email,
                PhoneNumber = model.PhoneNumber,
                // Accounts created by a SuperAdmin are trusted and must satisfy
                // the global RequireConfirmedAccount sign-in rule.
                EmailConfirmed = true,
            };

            var created = await _userManager.CreateAsync(user, model.Password);
            if (!created.Succeeded)
            {
                AddIdentityErrors(created);
                model.AvailableRoles = await GetRolesAsync(ct);
                return View(model);
            }

            var assigned = await _userManager.AddToRoleAsync(user, model.Role);
            if (!assigned.Succeeded)
            {
                AddIdentityErrors(assigned);
                await _userManager.DeleteAsync(user);
                model.AvailableRoles = await GetRolesAsync(ct);
                return View(model);
            }

            _logger.LogInformation("SuperAdmin created user {Email} with role {Role}.", user.Email, model.Role);
            TempData["SuccessMessage"] = "User created successfully.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(string id, CancellationToken ct = default)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user is null) return NotFound();

            var roles = await _userManager.GetRolesAsync(user);
            var model = new EditUserViewModel
            {
                Id = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                UserName = user.UserName ?? string.Empty,
                Email = user.Email ?? string.Empty,
                PhoneNumber = user.PhoneNumber,
                Role = roles.FirstOrDefault() ?? string.Empty,
                AvailableRoles = await GetRolesAsync(ct),
            };
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(EditUserViewModel model, CancellationToken ct = default)
        {
            var user = await _userManager.FindByIdAsync(model.Id);
            if (user is null) return NotFound();

            var roleError = await ValidateRoleChangeAsync(user, model.Role);
            if (roleError is not null)
            {
                ModelState.AddModelError(nameof(model.Role), roleError);
            }

            if (!ModelState.IsValid)
            {
                model.AvailableRoles = await GetRolesAsync(ct);
                return View(model);
            }

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(ct);

            async Task<IActionResult> FailAsync(
                IdentityResult? result = null,
                string? errorMessage = null)
            {
                if (result is not null) AddIdentityErrors(result);
                if (errorMessage is not null) ModelState.AddModelError(string.Empty, errorMessage);

                await transaction.RollbackAsync(ct);
                // Identity and the tracked user share this context; clear the rolled-back state
                // before the view is rendered again.
                _dbContext.ChangeTracker.Clear();
                model.AvailableRoles = await GetRolesAsync(ct);
                return View(model);
            }

            user.FirstName = model.FirstName;
            user.LastName = model.LastName;
            user.UserName = model.UserName;
            user.Email = model.Email;
            user.PhoneNumber = model.PhoneNumber;
            user.EmailConfirmed = true;

            var updated = await _userManager.UpdateAsync(user);
            if (!updated.Succeeded) return await FailAsync(updated);

            if (!string.IsNullOrWhiteSpace(model.NewPassword))
            {
                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                var reset = await _userManager.ResetPasswordAsync(user, token, model.NewPassword);
                if (!reset.Succeeded) return await FailAsync(reset);
            }

            var currentRoles = await _userManager.GetRolesAsync(user);
            var removed = await _userManager.RemoveFromRolesAsync(user, currentRoles);
            if (!removed.Succeeded) return await FailAsync(removed);

            IdentityResult assigned;
            try
            {
                assigned = await _userManager.AddToRoleAsync(user, model.Role);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "Role {Role} could not be assigned to user {UserId}.", model.Role, model.Id);
                return await FailAsync(errorMessage: "User could not be updated. Please try again.");
            }

            if (!assigned.Succeeded) return await FailAsync(assigned);

            await transaction.CommitAsync(ct);

            _logger.LogInformation("SuperAdmin updated user {Email} with role {Role}.", user.Email, model.Role);
            TempData["SuccessMessage"] = "User updated successfully.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> Delete(string id, CancellationToken ct = default)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user is null) return NotFound();

            if (user.Id == _userManager.GetUserId(User))
            {
                TempData["ErrorMessage"] = "You cannot delete your own account.";
                return RedirectToAction(nameof(Index));
            }

            var roles = await _userManager.GetRolesAsync(user);
            if (roles.Contains(SuperAdminRole) && await CountSuperAdminsAsync() <= 1)
            {
                TempData["ErrorMessage"] = "At least one SuperAdmin must remain.";
                return RedirectToAction(nameof(Index));
            }

            // Keep health-record audit snapshots but detach them from the deleted actor.
            await _dbContext.HealthRecordAudits
                .Where(audit => audit.ActorUserId == user.Id)
                .ExecuteUpdateAsync(
                    setters => setters.SetProperty(audit => audit.ActorUserId, (string?)null),
                    ct);

            var result = await _userManager.DeleteAsync(user);
            TempData[result.Succeeded ? "SuccessMessage" : "ErrorMessage"] = result.Succeeded
                ? "User deleted successfully."
                : string.Join("; ", result.Errors.Select(error => error.Description));

            return RedirectToAction(nameof(Index));
        }

        private async Task<List<string>> GetRolesAsync(CancellationToken ct) =>
            await _roleManager.Roles
                .AsNoTracking()
                .OrderBy(role => role.Name)
                .Select(role => role.Name!)
                .ToListAsync(ct);

        private async Task<string?> ValidateRoleChangeAsync(ApplicationUser user, string newRole)
        {
            if (!await _roleManager.RoleExistsAsync(newRole))
            {
                return "Selected role does not exist.";
            }

            var currentRoles = await _userManager.GetRolesAsync(user);
            if (currentRoles.Contains(SuperAdminRole)
                && newRole != SuperAdminRole
                && await CountSuperAdminsAsync() <= 1)
            {
                return "At least one SuperAdmin must remain.";
            }

            return null;
        }

        private async Task<int> CountSuperAdminsAsync() =>
            (await _userManager.GetUsersInRoleAsync(SuperAdminRole)).Count;

        private void AddIdentityErrors(IdentityResult result)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
        }
    }
}
