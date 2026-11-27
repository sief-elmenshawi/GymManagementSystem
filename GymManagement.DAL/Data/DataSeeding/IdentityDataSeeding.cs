using GymManagement.DAL.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace GymManagement.DAL.Data.DataSeeding
{
    public static class IdentityDataSeeding
    {
        private static readonly string[] Roles = { "SuperAdmin", "Admin", "Member" };

        public static async Task SeedAsync(
            RoleManager<IdentityRole> roleManager,
            UserManager<ApplicationUser> userManager,
            ILogger logger,
            string? adminPassword,
            CancellationToken ct = default)
        {
            try
            {
                await EnsureRolesAsync(roleManager, logger);

                if (string.IsNullOrWhiteSpace(adminPassword))
                {
                    logger.LogWarning(
                        "Default user password is not configured. Skipping user seeding. Set SeedUsers:Password or GYM_SEED_PASSWORD.");
                    return;
                }

                await EnsureUserAsync(
                    userManager,
                    logger,
                    new ApplicationUser
                    {
                        FirstName = "Serag",
                        LastName = "Ibrahim",
                        UserName = "Sergio",
                        Email = "Serag@gmail.com",
                        PhoneNumber = "01123456789",
                    },
                    adminPassword,
                    "SuperAdmin");

                await EnsureUserAsync(
                    userManager,
                    logger,
                    new ApplicationUser
                    {
                        FirstName = "Nader",
                        LastName = "Ibrahim",
                        UserName = "Nader",
                        Email = "Nader@gmail.com",
                        PhoneNumber = "01123457789",
                    },
                    adminPassword,
                    "Admin");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed To Seed Identity Data");
                throw;
            }
        }

        private static async Task EnsureRolesAsync(
            RoleManager<IdentityRole> roleManager,
            ILogger logger)
        {
            foreach (var roleName in Roles)
            {
                if (await roleManager.RoleExistsAsync(roleName))
                {
                    continue;
                }

                var result = await roleManager.CreateAsync(new IdentityRole(roleName));
                if (!result.Succeeded)
                {
                    throw new InvalidOperationException(
                        $"Failed to create role '{roleName}': {FormatErrors(result)}");
                }

                logger.LogInformation("Role {Role} seeded.", roleName);
            }
        }

        private static async Task EnsureUserAsync(
            UserManager<ApplicationUser> userManager,
            ILogger logger,
            ApplicationUser user,
            string password,
            string roleName)
        {
            var existing = await userManager.FindByEmailAsync(user.Email!);
            if (existing is null)
            {
                var created = await userManager.CreateAsync(user, password);
                if (!created.Succeeded)
                {
                    throw new InvalidOperationException(
                        $"Failed to seed user '{user.Email}': {FormatErrors(created)}");
                }

                existing = user;
                logger.LogInformation("User {Email} seeded.", user.Email);
            }

            if (await userManager.IsInRoleAsync(existing, roleName))
            {
                return;
            }

            var roleResult = await userManager.AddToRoleAsync(existing, roleName);
            if (!roleResult.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Failed to assign role '{roleName}' to '{user.Email}': {FormatErrors(roleResult)}");
            }

            logger.LogInformation("Role {Role} assigned to {Email}.", roleName, user.Email);
        }

        private static string FormatErrors(IdentityResult result) =>
            string.Join("; ", result.Errors.Select(error => $"{error.Code}: {error.Description}"));
    }
}
