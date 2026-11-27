namespace GymManagement.DAL.Models;

/// <summary>
/// Flat, no-tracking projection for the members list page.
/// Does not load the photo payload or the owned address.
/// </summary>
public sealed class MemberListInfo
{
    public int Id { get; set; }
    public string Name { get; set; } = default!;
    public string Email { get; set; } = default!;
    public string Phone { get; set; } = default!;
    public Enums.Gender Gender { get; set; }
    public bool HasPhoto { get; set; }
}
