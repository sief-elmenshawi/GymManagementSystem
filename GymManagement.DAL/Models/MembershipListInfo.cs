namespace GymManagement.DAL.Models;

/// <summary>
/// Flat, no-tracking projection for the active-memberships list page.
/// </summary>
public sealed class MembershipListInfo
{
    public int MemberId { get; set; }
    public int PlanId { get; set; }
    public string MemberName { get; set; } = default!;
    public string PlanName { get; set; } = default!;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
}
