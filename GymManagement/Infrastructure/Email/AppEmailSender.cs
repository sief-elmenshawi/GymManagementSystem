using System.Net;
using System.Net.Mail;
using GymManagement.BLL.Services.Interfaces;
using Microsoft.Extensions.Options;

namespace GymManagement.Infrastructure.Email;

/// <summary>
/// Uses SMTP when configured. In Development, unconfigured SMTP falls back to an
/// in-memory mailbox. In other environments a missing configuration is logged as
/// an error instead of leaking confirmation links or storing sensitive messages.
/// Failures are reported through <see cref="IEmailDeliveryMonitor"/>; delivery never
/// rolls back the database operation that requested it.
/// </summary>
public sealed class AppEmailSender : IAppEmailSender
{
    private readonly EmailOptions _options;
    private readonly DevelopmentMailbox _mailbox;
    private readonly IWebHostEnvironment _environment;
    private readonly IEmailDeliveryMonitor _monitor;

    public AppEmailSender(
        IOptions<EmailOptions> options,
        DevelopmentMailbox mailbox,
        IWebHostEnvironment environment,
        IEmailDeliveryMonitor monitor)
    {
        _options = options.Value;
        _mailbox = mailbox;
        _environment = environment;
        _monitor = monitor;
    }

    public async Task<bool> SendAsync(
        string toEmail,
        string subject,
        string htmlBody,
        CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(toEmail);

        var smtp = _options.Smtp;
        if (string.IsNullOrWhiteSpace(smtp.Host))
        {
            if (_environment.IsDevelopment())
            {
                _mailbox.Add(new DevelopmentEmailMessage(
                    toEmail,
                    subject,
                    htmlBody,
                    DateTimeOffset.UtcNow));
                _monitor.RecordSuccess();
                return true;
            }

            _monitor.RecordFailure(new EmailDeliveryFailure(
                EmailDeliveryFailureReason.NotConfigured,
                toEmail,
                subject,
                smtp.Host,
                Exception: null));
            return false;
        }

        try
        {
            using var message = new MailMessage
            {
                From = new MailAddress(_options.From),
                Subject = subject,
                Body = htmlBody,
                IsBodyHtml = true,
            };
            message.To.Add(new MailAddress(toEmail));

            using var client = new SmtpClient(smtp.Host, smtp.Port)
            {
                EnableSsl = smtp.UseSsl,
            };

            if (!string.IsNullOrWhiteSpace(smtp.User))
            {
                client.Credentials = new NetworkCredential(smtp.User, smtp.Password);
            }

            await client.SendMailAsync(message, ct);
            _monitor.RecordSuccess();
            return true;
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _monitor.RecordFailure(new EmailDeliveryFailure(
                EmailDeliveryFailureReason.SmtpError,
                toEmail,
                subject,
                smtp.Host,
                ex));
            return false;
        }
    }
}
