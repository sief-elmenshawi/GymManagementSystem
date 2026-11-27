namespace GymManagement.BLL.ViewModels.ApplicationViewModels;

public sealed class UserListViewModel
{
    public string Id { get; set; } = default!;
    public string UserName { get; set; } = default!;
    public string Email { get; set; } = default!;
    public string FirstName { get; set; } = default!;
    public string LastName { get; set; } = default!;
    public string? PhoneNumber { get; set; }
    public IReadOnlyList<string> Roles { get; set; } = Array.Empty<string>();
}
