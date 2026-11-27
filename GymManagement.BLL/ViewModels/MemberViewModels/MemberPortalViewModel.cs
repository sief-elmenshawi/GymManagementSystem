namespace GymManagement.BLL.ViewModels.MemberViewModels;

public sealed class MemberPortalViewModel
{
    public int MemberId { get; set; }
    public bool HasLinkedMember { get; set; }
    public string Name { get; set; } = default!;
    public string Email { get; set; } = default!;
    public string Phone { get; set; } = default!;
    public string? DateOfBirth { get; set; }
    public string? Gender { get; set; }
    public string? Address { get; set; }
    public bool HasHealthRecord { get; set; }
    public string? Notice { get; set; }
    public IReadOnlyList<MemberPortalBookingViewModel> Bookings { get; set; } =
        Array.Empty<MemberPortalBookingViewModel>();
}

public sealed class MemberPortalBookingViewModel
{
    public string Description { get; set; } = default!;
    public string Category { get; set; } = default!;
    public string Trainer { get; set; } = default!;
    public string StartDate { get; set; } = default!;
    public string EndDate { get; set; } = default!;
    public bool IsAttended { get; set; }
}
