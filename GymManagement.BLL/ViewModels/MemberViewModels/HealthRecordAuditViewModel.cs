namespace GymManagement.BLL.ViewModels.MemberViewModels;

public sealed class HealthRecordAuditViewModel
{
    public int Id { get; set; }
    public string Action { get; set; } = default!;
    public string ActorEmail { get; set; } = default!;
    public DateTime CreatedAt { get; set; }
    public decimal Height { get; set; }
    public decimal Weight { get; set; }
    public string BloodType { get; set; } = default!;
    public string? Note { get; set; }
}
