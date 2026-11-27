using System.ComponentModel.DataAnnotations;

namespace GymManagement.BLL.ViewModels.ApplicationViewModels;

public sealed class ForgotPasswordViewModel
{
    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Enter a valid email address.")]
    [Display(Name = "Email address")]
    public string Email { get; set; } = default!;
}
