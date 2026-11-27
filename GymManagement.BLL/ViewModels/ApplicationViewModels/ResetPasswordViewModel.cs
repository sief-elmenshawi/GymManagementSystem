using System.ComponentModel.DataAnnotations;

namespace GymManagement.BLL.ViewModels.ApplicationViewModels;

public sealed class ResetPasswordViewModel
{
    [Required]
    [EmailAddress]
    [Display(Name = "Email address")]
    public string Email { get; set; } = default!;

    [Required]
    public string Token { get; set; } = default!;

    [Required(ErrorMessage = "Password is required.")]
    [DataType(DataType.Password)]
    [StringLength(100, MinimumLength = 6)]
    [Display(Name = "New password")]
    public string Password { get; set; } = default!;

    [Required]
    [DataType(DataType.Password)]
    [Compare(nameof(Password), ErrorMessage = "Passwords do not match.")]
    [Display(Name = "Confirm new password")]
    public string ConfirmPassword { get; set; } = default!;
}
