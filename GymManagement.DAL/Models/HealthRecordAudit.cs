namespace GymManagement.DAL.Models;

/// <summary>
/// Append-only snapshot of every create/update/delete made to a health record.
/// Foreign keys are nullable so deleting a member or actor never destroys the
/// audit history; the service clears those links before the delete.
/// </summary>
public class HealthRecordAudit : BaseEntity
{
    public const string Created = "Created";
    public const string Updated = "Updated";
    public const string Deleted = "Deleted";

    public int? HealthRecordId { get; set; }
    public HealthRecord? HealthRecord { get; set; }

    public int? MemberId { get; set; }
    public Member? Member { get; set; }

    public string? ActorUserId { get; set; }
    public ApplicationUser? ActorUser { get; set; }

    public string ActorEmail { get; set; } = default!;
    public string Action { get; set; } = default!;

    public decimal Height { get; set; }
    public decimal Weight { get; set; }
    public string BloodType { get; set; } = default!;
    public string? Note { get; set; }
}
