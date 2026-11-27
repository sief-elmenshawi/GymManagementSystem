using GymManagement.BLL.MappingProfile;
using GymManagement.BLL.Service.Classes;
using GymManagement.BLL.Service.Interfaces;
using GymManagement.BLL.Services.Classes;
using GymManagement.BLL.Services.Interfaces;
using GymManagement.DAL.Data.DbContexts;
using GymManagement.DAL.Models;
using GymManagement.DAL.Repositorities.Classes;
using GymManagement.DAL.Repositorities.Interfaces;
using GymManagement.PL;
using GymManagement.Infrastructure.Email;

using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GymManagementPL
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // AutoMapper 15+ logs a license warning in non-production environments.
            // Production reads the key from configuration or the AUTOMAPPER_LICENSE_KEY env var.
            if (builder.Environment.IsDevelopment())
            {
                builder.Logging.AddFilter("LuckyPennySoftware.AutoMapper.License", LogLevel.Error);
            }

            // Add services to the container.
            builder.Services.AddControllersWithViews(options =>
                options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute()));

            builder.Services.Configure<EmailOptions>(
                builder.Configuration.GetSection(EmailOptions.SectionName));
            builder.Services.AddSingleton<DevelopmentMailbox>();
            builder.Services.AddScoped<IAppEmailSender, AppEmailSender>();

            builder.Services.AddDbContext<GymDbContext>(options =>
            {
                options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
            });

            #region Repositories + UoW

            builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
            builder.Services.AddScoped<IMembershipRepository, MembershipRepository>();
            builder.Services.AddScoped<IMemberRepository, MemberRepository>();
            builder.Services.AddScoped<ISessionRepository, SessionRepository>();
            builder.Services.AddScoped<IBookingRepository, BookingRepository>();

            #endregion

            #region Domain services


            builder.Services.AddScoped<IMemberService, MemberService>();
            builder.Services.AddScoped<ITrainerService, TrainerService>();
            builder.Services.AddScoped<IPlanService, PlanService>();
            builder.Services.AddScoped<ISessionService, SessionService>();
            builder.Services.AddScoped<IMembershipService, MembershipService>();
            builder.Services.AddScoped<IAnalyticsService, AnalyticsService>();
            builder.Services.AddScoped<IBookingService, BookingService>();
            builder.Services.AddScoped<IAttachementService, AttachementService>();


            #endregion

            var autoMapperLicenseKey = builder.Environment.IsDevelopment()
                ? AutoMapperLicense.ResolveKey(builder.Configuration)
                : AutoMapperLicense.RequireKey(builder.Configuration, builder.Environment.EnvironmentName);

            builder.Services.AddAutoMapper(cfg =>
            {
                // Development allows a missing key; non-development environments fail fast above.
                if (autoMapperLicenseKey is not null)
                {
                    cfg.LicenseKey = autoMapperLicenseKey;
                }

                cfg.AddProfile<MappingProfile>();
            });

            builder.Services.AddIdentity<ApplicationUser, IdentityRole>(Config =>
            {
                //Config.Password.RequiredLength = 6;
                //Config.Password.RequireLowercase = true;
                //Config.Password.RequireUppercase = true;
                Config.User.RequireUniqueEmail = true;
                Config.SignIn.RequireConfirmedAccount = true;
                Config.SignIn.RequireConfirmedEmail = true;
                Config.Lockout.MaxFailedAccessAttempts = 5;
                Config.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(2);

            })
            .AddEntityFrameworkStores<GymDbContext>()
            .AddDefaultTokenProviders();

            builder.Services.ConfigureApplicationCookie(options =>
            {
                // redirect unauthenticated users (401)
                options.LoginPath = "/Account/Login";
                // redirect forbidden users (403)
                options.AccessDeniedPath = "/Account/AccessDenied";
            });// Default Paths

            var httpsPort = HostingConfiguration.GetHttpsPort(builder.Configuration);
            if (httpsPort.HasValue)
            {
                builder.Services.Configure<HttpsRedirectionOptions>(options => options.HttpsPort = httpsPort.Value);
            }

            var app = builder.Build();

            if (!app.Environment.IsDevelopment()
                && SeedCredentials.ResolvePassword(builder.Configuration) is null)
            {
                app.Logger.LogWarning(
                    "Seed admin password is not configured in {Environment} environment. " +
                    "Set {Variable} or {ConfigKey} to seed the default users; seeding will be skipped otherwise.",
                    app.Environment.EnvironmentName,
                    SeedCredentials.EnvironmentVariable,
                    SeedCredentials.ConfigurationKey);
            }

            await app.MigrateAndSeedDatabaseAsync();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            // Only redirect when an HTTPS endpoint/port is actually configured.
            if (HostingConfiguration.ShouldUseHttpsRedirection(builder.Configuration))
            {
                app.UseHttpsRedirection();
            }

            app.UseRouting();
            app.UseAuthentication();
            app.UseAuthorization();

            app.MapStaticAssets();

            // Development-only mailbox for confirmation/reset links. Never mapped in production.
            if (app.Environment.IsDevelopment())
            {
                app.MapGet("/dev-mailbox/{email}", (string email, DevelopmentMailbox mailbox) =>
                    Results.Ok(mailbox.GetByEmail(email)));
                app.MapDelete("/dev-mailbox", (DevelopmentMailbox mailbox) =>
                {
                    mailbox.Clear();
                    return Results.NoContent();
                });
            }

            // Explicit Home route replaces the old catch-all "{controller}/{id?}", which
            // silently sent URLs such as /Members/999 to MembersController.Index.
            app.MapControllerRoute(
                name: "home",
                pattern: "Home/{action=Index}/{id?}",
                defaults: new { controller = "Home" });
            app.MapControllerRoute(
                name: "dashboard",
                pattern: "Dashboard/{action=Index}/{id?}",
                defaults: new { controller = "Dashboard" });
            app.MapControllerRoute(
                name: "users",
                pattern: "Users/{action=Index}/{id?}",
                defaults: new { controller = "Users" });
            app.MapControllerRoute(
                name: "member",
                pattern: "Member/{action=Index}/{id?}",
                defaults: new { controller = "MemberPortal" });
            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Account}/{action=Login}/{id?}");

            await app.RunAsync();
        }
    }
}