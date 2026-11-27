using GymManagement.DAL.Models.Enums;
using System.ComponentModel.DataAnnotations;

namespace GymManagement.BLL.ViewModels.ApplicationViewModels;

public class RegisterViewModel
{
    [Required]
    [StringLength(24)]
    [Display(Name = "First name")]
    public string FirstName { get; set; } = default!;

    [Required]
    [StringLength(24)]
    [Display(Name = "Last name")]
    public string LastName { get; set; } = default!;

    [Required]
    [EmailAddress]
    [StringLength(100)]
    public string Email { get; set; } = default!;

    [Required]
    [RegularExpression(@"^(010|011|012|015)\d{8}$",
        ErrorMessage = "Enter a valid Egyptian phone number.")]
    [StringLength(11)]
    [Display(Name = "Phone number")]
    public string Phone { get; set; } = default!;

    [Required]
    [DataType(DataType.Date)]
    [Display(Name = "Date of birth")]
    public DateOnly? DateOfBirth { get; set; }

    [Required]
    [Display(Name = "Gender")]
    public Gender? Gender { get; set; }

    [Required]
    [StringLength(100)]
    public string Street { get; set; } = default!;

    [Required]
    [StringLength(100)]
    public string City { get; set; } = default!;

    [Required]
    [Range(1, int.MaxValue)]
    [Display(Name = "Building number")]
    public int BuildingNumber { get; set; }

    [Required]
    [DataType(DataType.Password)]
    [StringLength(100, MinimumLength = 6)]
    public string Password { get; set; } = default!;

    [Required]
    [DataType(DataType.Password)]
    [Compare(nameof(Password))]
    [Display(Name = "Confirm password")]
    public string ConfirmPassword { get; set; } = default!;
}
