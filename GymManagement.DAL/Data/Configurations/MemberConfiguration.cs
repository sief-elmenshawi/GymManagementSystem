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
    public class MemberConfiguration : GymUserConfiguration<Member>, IEntityTypeConfiguration<Member>
    {
        public new void Configure(EntityTypeBuilder<Member> builder)
        {
            builder.Property(x => x.CreatedAt)
              .HasColumnName("JoinDate")
              .HasDefaultValueSql("GETUTCDATE()");

            builder.Property(x => x.ApplicationUserId)
              .HasMaxLength(450);

            builder.HasIndex(x => x.ApplicationUserId)
              .IsUnique()
              .HasFilter("[ApplicationUserId] IS NOT NULL");

            builder.HasOne(x => x.ApplicationUser)
              .WithOne(x => x.Member)
              .HasForeignKey<Member>(x => x.ApplicationUserId)
              .OnDelete(DeleteBehavior.SetNull);

            base.Configure(builder);
        }
    }
}