using GymManagement.DAL.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GymManagement.DAL.Data.Configurations
{
    public class SessionConfiguration : IEntityTypeConfiguration<Session>
    {
        public void Configure(EntityTypeBuilder<Session> builder)
        {
            builder.ToTable(tb =>
            {
                tb.HasCheckConstraint("SessionCapacityCheck", "Capacity Between 1and 25 ");
                tb.HasCheckConstraint("SessionEndDateCheck", "EndDate > StartDate ");
            });

            // Dashboard and booking lists filter on these ranges.
            builder.HasIndex(s => new { s.StartDate, s.EndDate });
            builder.HasIndex(s => s.EndDate);
        }
    }
}