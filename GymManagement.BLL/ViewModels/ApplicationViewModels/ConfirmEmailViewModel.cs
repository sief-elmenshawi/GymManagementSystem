namespace GymManagement.BLL.ViewModels.ApplicationViewModels;

public sealed class ConfirmEmailViewModel
{
    public bool Succeeded { get; set; }

    public string Message { get; set; } = string.Empty;
}
