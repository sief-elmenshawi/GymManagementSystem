using System.Net;
using GymManagement;
using GymManagement.BLL.Services.Interfaces;
using GymManagement.BLL.ViewModels.ApplicationViewModels;
using GymManagement.Controllers;
using GymManagement.DAL.Data.DbContexts;
using GymManagement.DAL.Models;
using GymManagement.Infrastructure.RateLimiting;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
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
        private readonly IAppEmailSender _emailSender;
        private readonly ILogger<AccountController> _logger;

        public AccountController(
            SignInManager<ApplicationUser> signInManager,
            UserManager<ApplicationUser> userManager,
            GymDbContext dbContext,
            IAppEmailSender emailSender,
            ILogger<AccountController> logger)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _dbContext = dbContext;
            _emailSender = emailSender;
            _logger = logger;
        }
        #region Login
        [AllowAnonymous]
        public IActionResult Login()
        {
            return View();
        }
        [HttpPost]
        [AllowAnonymous]
        [EnableRateLimiting(AuthenticationRateLimiting.PolicyName)]
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
                ModelState.AddModelError(
                    "InvalidLogin",
                    "Please confirm your email address before signing in. " +
                    "Check your inbox for the confirmation link.");
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
        [EnableRateLimiting(AuthenticationRateLimiting.PolicyName)]
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

            try
            {
                // Identity and gym data share this scoped context, so one transaction
                // keeps user, role, and member creation atomic.
                await using var transaction = await _dbContext.Database.BeginTransactionAsync(ct);

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
                    ModelState.AddIdentityErrors(created);
                    await transaction.RollbackAsync(ct);
                    return View(model);
                }

                IdentityResult roleResult;
                try
                {
                    roleResult = await _userManager.AddToRoleAsync(user, MemberRole);
                }
                catch (InvalidOperationException ex)
                {
                    // UserStore throws when the seeded role is missing instead of returning a result.
                    _logger.LogError(ex, "The {Role} role is missing; registration was rolled back.", MemberRole);
                    ModelState.AddModelError(
                        string.Empty,
                        "Registration could not be completed. Please try again.");
                    await transaction.RollbackAsync(ct);
                    return View(model);
                }

                if (!roleResult.Succeeded)
                {
                    ModelState.AddIdentityErrors(roleResult);
                    await transaction.RollbackAsync(ct);
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
                await _dbContext.SaveChangesAsync(ct);
                await transaction.CommitAsync(ct);

                // Email delivery is outside the database transaction by design.
                var confirmationDelivered = await SendConfirmationEmailAsync(user, ct);

                _logger.LogInformation(
                    "Registered member {Email}; confirmation delivery succeeded: {Delivered}.",
                    user.Email,
                    confirmationDelivered);
                TempData["SuccessMessage"] = confirmationDelivered
                    ? "Registration successful. Check your email to confirm your account before signing in."
                    : "Registration successful, but we could not send the confirmation email. " +
                      "Contact support before signing in.";
                return Redirect("/Account/Login");
            }
            catch (DbUpdateException ex)
            {
                _dbContext.ChangeTracker.Clear();
                _logger.LogError(ex, "Failed to complete registration for {Email}.", model.Email);
                ModelState.AddModelError(string.Empty, "Registration could not be completed. Please try again.");
                return View(model);
            }
        }

        #region Email confirmation

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> ConfirmEmail(string? userId, string? token)
        {
            if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(token))
            {
                return View(new ConfirmEmailViewModel
                {
                    Succeeded = false,
                    Message = "This confirmation link is invalid or incomplete.",
                });
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user is null)
            {
                return View(new ConfirmEmailViewModel
                {
                    Succeeded = false,
                    Message = "We could not find the account for this confirmation link.",
                });
            }

            var result = await _userManager.ConfirmEmailAsync(user, token);
            if (result.Succeeded)
            {
                _logger.LogInformation("Email confirmed for user {UserId}.", user.Id);
            }
            else
            {
                ModelState.AddIdentityErrors(result);
            }

            return View(new ConfirmEmailViewModel
            {
                Succeeded = result.Succeeded,
                Message = result.Succeeded
                    ? "Your email address is confirmed. You can now sign in."
                    : "We could not confirm this email address. The link may have expired. " +
                      "Request a new link below.",
            });
        }

        #endregion

        #region Resend email confirmation

        [HttpGet]
        [AllowAnonymous]
        public IActionResult ResendEmailConfirmation() => View();

        [HttpPost]
        [AllowAnonymous]
        [EnableRateLimiting(AuthenticationRateLimiting.PolicyName)]
        public async Task<IActionResult> ResendEmailConfirmation(
            ResendEmailConfirmationViewModel model,
            CancellationToken ct = default)
        {
            if (!ModelState.IsValid) return View(model);

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user is not null && !await _userManager.IsEmailConfirmedAsync(user))
            {
                var delivered = await SendConfirmationEmailAsync(user, ct);
                _logger.LogInformation(
                    "Confirmation link reissued for user {UserId}; delivery succeeded: {Delivered}.",
                    user.Id,
                    delivered);
            }

            // Same non-disclosure response as ForgotPassword. Delivery failures are
            // logged and alerted through IEmailDeliveryMonitor without revealing whether
            // the address exists.
            TempData["SuccessMessage"] =
                "If an unconfirmed account exists for that email, a new confirmation link " +
                "has been sent. If you do not receive it, contact support.";
            return RedirectToAction(nameof(ResendEmailConfirmationConfirmation));
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult ResendEmailConfirmationConfirmation() => View();

        #endregion

        #region Forgot password

        [HttpGet]
        [AllowAnonymous]
        public IActionResult ForgotPassword() => View();

        [HttpPost]
        [AllowAnonymous]
        [EnableRateLimiting(AuthenticationRateLimiting.PolicyName)]
        public async Task<IActionResult> ForgotPassword(
            ForgotPasswordViewModel model,
            CancellationToken ct = default)
        {
            if (!ModelState.IsValid) return View(model);

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user is not null)
            {
                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                var callbackUrl = Url.Action(
                    nameof(ResetPassword),
                    "Account",
                    new { email = user.Email, token },
                    Request.Scheme) ?? "/Account/ResetPassword";

                var delivered = await _emailSender.SendAsync(
                    user.Email!,
                    "Reset your gym account password",
                    $"<p>Hello {WebUtility.HtmlEncode(user.FirstName)},</p>" +
                    $"<p>Use the link below to reset your password. " +
                    $"If you did not request this, you can ignore this email.</p>" +
                    $"<p><a href=\"{WebUtility.HtmlEncode(callbackUrl)}\">Reset password</a></p>",
                    ct);

                _logger.LogInformation(
                    "Password reset link issued for user {UserId}; delivery succeeded: {Delivered}.",
                    user.Id,
                    delivered);
            }

            TempData["SuccessMessage"] =
                "If an account exists for that email, a password reset link has been sent.";
            return RedirectToAction(nameof(ForgotPasswordConfirmation));
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult ForgotPasswordConfirmation() => View();

        #endregion

        #region Reset password

        [HttpGet]
        [AllowAnonymous]
        public IActionResult ResetPassword(string? email, string? token)
        {
            var model = new ResetPasswordViewModel
            {
                Email = email ?? string.Empty,
                Token = token ?? string.Empty,
            };

            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(token))
            {
                ModelState.AddModelError(string.Empty, "This password reset link is invalid or incomplete.");
            }

            return View(model);
        }

        [HttpPost]
        [AllowAnonymous]
        [EnableRateLimiting(AuthenticationRateLimiting.PolicyName)]
        public async Task<IActionResult> ResetPassword(
            ResetPasswordViewModel model,
            CancellationToken ct = default)
        {
            if (!ModelState.IsValid) return View(model);

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user is null)
            {
                return RedirectToAction(nameof(ResetPasswordConfirmation));
            }

            var result = await _userManager.ResetPasswordAsync(user, model.Token, model.Password);
            if (!result.Succeeded)
            {
                ModelState.AddIdentityErrors(result);
                return View(model);
            }

            _logger.LogInformation("Password reset completed for user {UserId}.", user.Id);
            return RedirectToAction(nameof(ResetPasswordConfirmation));
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult ResetPasswordConfirmation() => View();

        #endregion

        private async Task<bool> SendConfirmationEmailAsync(ApplicationUser user, CancellationToken ct)
        {
            var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            var callbackUrl = Url.Action(
                nameof(ConfirmEmail),
                "Account",
                new { userId = user.Id, token },
                Request.Scheme) ?? "/Account/ConfirmEmail";

            return await _emailSender.SendAsync(
                user.Email!,
                "Confirm your gym account",
                $"<p>Welcome to the gym, {WebUtility.HtmlEncode(user.FirstName)}.</p>" +
                $"<p>Please confirm your email address before signing in:</p>" +
                $"<p><a href=\"{WebUtility.HtmlEncode(callbackUrl)}\">Confirm email address</a></p>",
                ct);
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
        [AllowAnonymous]
        public IActionResult AccessDenied() => View();

    }

}

