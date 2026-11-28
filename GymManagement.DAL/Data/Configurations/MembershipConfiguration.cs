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
    internal class MembershipConfiguration : IEntityTypeConfiguration<Membership>
    {
        public void Configure(EntityTypeBuilder<Membership> builder)
        {
            builder.HasKey(x => x.Id);

            builder.Property(x => x.CreatedAt).HasColumnName("StartDate").HasDefaultValueSql("GetDate()");

            // Active-membership lookups always combine an id with EndDate.
            builder.HasIndex(m => new { m.MemberId, m.EndDate });
            builder.HasIndex(m => new { m.PlanId, m.EndDate });
            builder.HasIndex(m => m.EndDate);
        }
    }
}