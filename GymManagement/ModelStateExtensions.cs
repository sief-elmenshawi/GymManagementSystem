using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace GymManagement;

/// <summary>
/// Copies Identity validation errors into ModelState from one shared place instead of
/// repeating the same loop in every controller.
/// </summary>
public static class ModelStateExtensions
{
    public static void AddIdentityErrors(this ModelStateDictionary modelState, IdentityResult result)
    {
        foreach (var error in result.Errors)
        {
            modelState.AddModelError(string.Empty, error.Description);
        }
    }
}
