using System.ComponentModel.DataAnnotations;

namespace GymManagement.BLL.ViewModels.ApplicationViewModels;

public class UserFormViewModel
{
    [Required]
    [StringLength(100)]
    [Display(Name = "First name")]
    public string FirstName { get; set; } = default!;

    [Required]
    [StringLength(100)]
    [Display(Name = "Last name")]
    public string LastName { get; set; } = default!;

    [Required]
    [StringLength(256)]
    [Display(Name = "User name")]
    public string UserName { get; set; } = default!;

    [Required]
    [EmailAddress]
    [StringLength(256)]
    public string Email { get; set; } = default!;

    [Phone]
    [Display(Name = "Phone number")]
    public string? PhoneNumber { get; set; }

    [Required]
    public string Role { get; set; } = default!;

    public IReadOnlyList<string> AvailableRoles { get; set; } = Array.Empty<string>();
}

public sealed class CreateUserViewModel : UserFormViewModel
{
    [Required]
    [DataType(DataType.Password)]
    [StringLength(100, MinimumLength = 6)]
    [Display(Name = "Password")]
    public string Password { get; set; } = default!;
}

public sealed class EditUserViewModel : UserFormViewModel
{
    public string Id { get; set; } = default!;

    [DataType(DataType.Password)]
    [Display(Name = "New password (optional)")]
    public string? NewPassword { get; set; }
}
