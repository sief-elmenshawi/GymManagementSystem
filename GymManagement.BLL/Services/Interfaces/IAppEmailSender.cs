namespace GymManagement.BLL.Services.Interfaces;

/// <summary>
/// Sends transactional account emails (confirmation and password reset).
/// Implementations decide whether delivery uses SMTP or the local development mailbox.
/// Returns true when the message was delivered or stored in the Development mailbox;
/// failures are recorded by the delivery monitor but never throw back into the caller.
/// </summary>
public interface IAppEmailSender
{
    Task<bool> SendAsync(
        string toEmail,
        string subject,
        string htmlBody,
        CancellationToken ct = default);
}
