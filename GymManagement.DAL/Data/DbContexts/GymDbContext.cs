using GymManagement.DAL.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.Reflection;

namespace GymManagement.DAL.Data.DbContexts
{
    public class GymDbContext : IdentityDbContext<ApplicationUser>
    {
        public GymDbContext(DbContextOptions<GymDbContext> options) : base(options)
        {

        }
        //protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        //{
        //    optionsBuilder.UseSqlServer("Server=.;Database=GymManagement;Trusted_Connection=True;TrustServerCertificate=True;");
        //}
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder); // Identity Configurations
            modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly()); // Application Configurations

        }

        #region DbSets
        public DbSet<Plan> Plans { get; set; }
        public DbSet<Member> Members { get; set; }
        public DbSet<Address> Addresses { get; set; }
        public DbSet<Booking> Bookings { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<HealthRecord> HealthRecords { get; set; }
        public DbSet<HealthRecordAudit> HealthRecordAudits { get; set; }
        public DbSet<Membership> Memberships { get; set; }
        public DbSet<Session> Sessions { get; set; }
        public DbSet<Trainer> Trainers { get; set; }

        #endregion


    }
}