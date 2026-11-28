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
    internal class BookingConfeguration : IEntityTypeConfiguration<Booking>
    {
        public void Configure(EntityTypeBuilder<Booking> builder)
        {
            builder.Property(x => x.CreatedAt)
                .HasColumnName("BookingDate")
                .HasDefaultValueSql("GetDate()");

            builder.HasKey(x => x.Id);

            builder.HasIndex(x => new { x.MemberId, x.SessionId })
                .IsUnique()
                .HasDatabaseName("UX_Bookings_MemberId_SessionId");

        }
    }
}