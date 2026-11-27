namespace GymManagement.DAL.Models;

/// <summary>
/// Flat, no-tracking projection used by list pages. Booked slots are counted in SQL
/// so a list of N sessions costs one round trip instead of N+1.
/// </summary>
public sealed class SessionListInfo
{
    public int Id { get; set; }
    public string Description { get; set; } = default!;
    public int Capacity { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string TrainerName { get; set; } = default!;
    public string CategoryName { get; set; } = default!;
    public int AvailableSlots { get; set; }
}
