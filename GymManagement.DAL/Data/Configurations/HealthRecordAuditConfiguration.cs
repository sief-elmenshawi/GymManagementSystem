using GymManagement.DAL.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GymManagement.DAL.Data.Configurations;

internal class HealthRecordAuditConfiguration : IEntityTypeConfiguration<HealthRecordAudit>
{
    public void Configure(EntityTypeBuilder<HealthRecordAudit> builder)
    {
        builder.Property(x => x.Action).HasMaxLength(16).IsRequired();
        builder.Property(x => x.ActorEmail).HasMaxLength(256).IsRequired();
        builder.Property(x => x.ActorUserId).HasMaxLength(450);
        builder.Property(x => x.BloodType).HasMaxLength(5).IsRequired();
        builder.Property(x => x.Note).HasMaxLength(500);
        builder.Property(x => x.Height).HasPrecision(18, 4);
        builder.Property(x => x.Weight).HasPrecision(18, 4);

        builder.HasIndex(x => x.MemberId);
        builder.HasIndex(x => x.HealthRecordId);
        builder.HasIndex(x => x.CreatedAt);

        builder.HasOne(x => x.HealthRecord)
            .WithMany()
            .HasForeignKey(x => x.HealthRecordId)
            .OnDelete(DeleteBehavior.SetNull);

        // SQL Server rejects a second cascade path from Members/AspNetUsers through
        // HealthRecords. The service clears these FKs explicitly before deleting
        // a member or an actor, preserving the audit snapshot.
        builder.HasOne(x => x.Member)
            .WithMany()
            .HasForeignKey(x => x.MemberId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(x => x.ActorUser)
            .WithMany()
            .HasForeignKey(x => x.ActorUserId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
