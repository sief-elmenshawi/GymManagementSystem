namespace GymManagement.BLL.Services.Interfaces;

/// <summary>
/// Sends transactional account emails (confirmation and password reset).
/// Implementations decide whether delivery uses SMTP or the local development mailbox.
/// </summary>
public interface IAppEmailSender
{
    Task SendAsync(
        string toEmail,
        string subject,
        string htmlBody,
        CancellationToken ct = default);
}
