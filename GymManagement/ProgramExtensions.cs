using GymManagement.DAL.Data.DataSeeding;
using GymManagement.DAL.Data.DbContexts;
using GymManagement.DAL.Models;
using GymManagementPL;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace GymManagement.PL
{
    public static class ProgramExtensions
    {
        public static async Task MigrateAndSeedDatabaseAsync(this WebApplication app)
        {


            using var scope = app.Services.CreateScope();

            var dbContext = scope.ServiceProvider.GetRequiredService<GymDbContext>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
            var Configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();


            var pendingMigrations = await dbContext.Database.GetPendingMigrationsAsync();

            if (pendingMigrations.Any())
            {
                logger.LogInformation($"Applying {pendingMigrations.Count()} Pending Migrations");
                await dbContext.Database.MigrateAsync();
            }

            var seedFolderPath = Path.Combine(app.Environment.ContentRootPath, "wwwroot", "Files");
            await GymDataSeeding.SeedAsync(dbContext, seedFolderPath, logger);

            var isDevelopment = app.Environment.IsDevelopment();
            var userSeedingEnabled = SeedCredentials.ResolveUserSeedingEnabled(Configuration, isDevelopment);
            var seedPassword = userSeedingEnabled
                ? SeedCredentials.ResolvePassword(Configuration)
                : null;

            if (!isDevelopment && !userSeedingEnabled)
            {
                logger.LogWarning(
                    "Default user seeding is disabled. Set {Variable}=true or {ConfigKey}=true only for " +
                    "a deliberate bootstrap; the seeded accounts use predictable identities.",
                    SeedCredentials.EnabledEnvironmentVariable,
                    SeedCredentials.EnabledConfigurationKey);
            }

            await IdentityDataSeeding.SeedAsync(
                roleManager,
                userManager,
                logger,
                seedPassword,
                warnOnPredictableAccounts: !isDevelopment && userSeedingEnabled);
        }
    }
}
