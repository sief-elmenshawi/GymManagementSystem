using GymManagement.BLL.ViewModels.ApplicationViewModels;
using GymManagement.Controllers;
using GymManagement.DAL.Data.DbContexts;
using GymManagement.DAL.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GymManagement.PL.Controllers
{
    public class AccountController : Controller 
    {
        private const string MemberRole = "Member";

        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly GymDbContext _dbContext;
        private readonly ILogger<AccountController> _logger;

        public AccountController(
            SignInManager<ApplicationUser> signInManager,
            UserManager<ApplicationUser> userManager,
            GymDbContext dbContext,
            ILogger<AccountController> logger)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _dbContext = dbContext;
            _logger = logger;
        }
        #region Login
        public IActionResult Login()
        {
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model, CancellationToken ct = default)
        {
            if (!ModelState.IsValid) return View(model);

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user is null || string.IsNullOrEmpty(user.UserName))
            {
                ModelState.AddModelError("InvalidLogin", "Invalid email or password.");
                return View(model);
            }

            var result = await _signInManager.PasswordSignInAsync(
                user.UserName, model.Password, model.RememberMe, lockoutOnFailure: true);

            if (result.Succeeded)
            {
                _logger.LogInformation("User {UserId} signed in.", user.Id);
                return RedirectToAction(nameof(HomeController.Index), "Home");
            }
            if (result.IsLockedOut)
            {
                _logger.LogWarning("User {UserId} is locked out.", user.Id);
                ModelState.AddModelError("InvalidLogin", "This account is temporarily locked. Try again later.");
            }
            else if (result.IsNotAllowed)
            {
                ModelState.AddModelError("InvalidLogin", "Sign-in is not allowed for this account.");
            }
            else
            {
                ModelState.AddModelError("InvalidLogin", "Invalid email or password.");
            }
            return View(model);
        }

        #endregion

        #region Register

        [AllowAnonymous]
        public IActionResult Register() => View();

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> Register(RegisterViewModel model, CancellationToken ct = default)
        {
            if (!ModelState.IsValid) return View(model);

            if (await _userManager.FindByEmailAsync(model.Email) is not null ||
                await _dbContext.Members.AsNoTracking().AnyAsync(member => member.Email == model.Email, ct))
            {
                ModelState.AddModelError(nameof(model.Email), "An account with this email already exists.");
            }

            if (await _dbContext.Members.AsNoTracking().AnyAsync(member => member.Phone == model.Phone, ct))
            {
                ModelState.AddModelError(nameof(model.Phone), "A member with this phone number already exists.");
            }

            if (!ModelState.IsValid) return View(model);

            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                FirstName = model.FirstName,
                LastName = model.LastName,
                PhoneNumber = model.Phone,
            };

            var created = await _userManager.CreateAsync(user, model.Password);
            if (!created.Succeeded)
            {
                AddIdentityErrors(created);
                return View(model);
            }

            var roleResult = await _userManager.AddToRoleAsync(user, MemberRole);
            if (!roleResult.Succeeded)
            {
                AddIdentityErrors(roleResult);
                await _userManager.DeleteAsync(user);
                return View(model);
            }

            var member = new Member
            {
                ApplicationUserId = user.Id,
                Name = $"{model.FirstName} {model.LastName}",
                Email = model.Email,
                Phone = model.Phone,
                DateOfBirth = model.DateOfBirth!.Value,
                Gender = model.Gender!.Value,
                Photo = string.Empty,
                Address = new Address
                {
                    Street = model.Street,
                    City = model.City,
                    BuildingNumber = model.BuildingNumber,
                },
            };

            _dbContext.Members.Add(member);

            try
            {
                await _dbContext.SaveChangesAsync(ct);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Failed to create member record for registered user {UserId}.", user.Id);
                _dbContext.Entry(member).State = EntityState.Detached;
                await _userManager.DeleteAsync(user);
                ModelState.AddModelError(string.Empty, "Registration could not be completed. Please try again.");
                return View(model);
            }

            _logger.LogInformation("Registered member {Email}.", user.Email);
            TempData["SuccessMessage"] = "Registration successful. Please sign in.";
            return Redirect("/Account/Login");
        }

        private void AddIdentityErrors(IdentityResult result)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
        }

        #endregion

        #region Sign Out

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction(nameof(Login));
        }
        #endregion
        public IActionResult AccessDenied() => View();

    }

}

