namespace GymManagement.Infrastructure.Email;

public enum EmailDeliveryFailureReason
{
    NotConfigured,
    SmtpError,
}

public sealed record EmailDeliveryFailure(
    EmailDeliveryFailureReason Reason,
    string ToEmail,
    string Subject,
    string? SmtpHost,
    Exception? Exception);

/// <summary>
/// Operational hook for email delivery outcomes. The default implementation emits
/// structured logs and metrics; a deployment can replace it with one that also
/// forwards alerts to its own incident system.
/// </summary>
public interface IEmailDeliveryMonitor
{
    void RecordSuccess();

    void RecordFailure(EmailDeliveryFailure failure);
}
