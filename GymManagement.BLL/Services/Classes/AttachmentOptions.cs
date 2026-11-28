namespace GymManagement.BLL.Services.Classes;

/// <summary>
/// Upload rules read from the Attachments configuration section. Defaults match
/// the current UI text (JPG/PNG/GIF, 5 MB).
/// </summary>
public sealed class AttachmentOptions
{
    public const string SectionName = "Attachments";

    public long MaxFileSizeBytes { get; set; } = 5 * 1024 * 1024;

    public string[] AllowedExtensions { get; set; } = { ".jpg", ".jpeg", ".png", ".gif" };
}
