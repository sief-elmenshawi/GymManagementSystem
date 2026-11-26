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

using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Identity;
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
                builder.Logging.AddFilter("LuckyPennySoftware.AutoMapper.License", LogLevel.None);
            }

            // Add services to the container.
            builder.Services.AddControllersWithViews();

            builder.Services.AddDbContext<GymDbContext>(options =>
            {
                options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
            });

            #region Repositories + UoW

            builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
            builder.Services.AddScoped<IMembershipRepository, MembershipRepository>();
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

            builder.Services.AddAutoMapper(cfg =>
            {
                cfg.LicenseKey = builder.Configuration["AutoMapper:LicenseKey"];
                cfg.AddProfile<MappingProfile>();
            });

            builder.Services.AddIdentity<ApplicationUser, IdentityRole>(Config =>
            {
                //Config.Password.RequiredLength = 6;
                //Config.Password.RequireLowercase = true;
                //Config.Password.RequireUppercase = true;
                Config.User.RequireUniqueEmail = true;
                Config.Lockout.MaxFailedAccessAttempts = 5;
                Config.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(2);

            }).AddEntityFrameworkStores<GymDbContext>();

            builder.Services.ConfigureApplicationCookie(options =>
            {
                // redirect unauthenticated users (401)
                options.LoginPath = "/Account/Login";
                // redirect forbidden users (403)
                options.AccessDeniedPath = "/Account/AccessDenied";
            });// Default Paths

            var httpsPort = builder.Configuration.GetValue<int?>("HttpsPort");
            if (httpsPort.HasValue)
            {
                builder.Services.Configure<HttpsRedirectionOptions>(options => options.HttpsPort = httpsPort.Value);
            }

            var app = builder.Build();

            await app.MigrateAndSeedDatabaseAsync();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            // Only redirect when an HTTPS endpoint/port is actually configured.
            if (httpsPort.HasValue || app.Urls.Any(url => url.StartsWith("https", StringComparison.OrdinalIgnoreCase)))
            {
                app.UseHttpsRedirection();
            }

            app.UseRouting();
            app.UseAuthentication();
            app.UseAuthorization();

            app.MapStaticAssets();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Account}/{action=Login}/{id?}");
            app.MapControllerRoute(
                name: "controllerDefault",
                pattern: "{controller}/{id?}",
                defaults: new { action = "Index" });

            await app.RunAsync();
        }
    }
}