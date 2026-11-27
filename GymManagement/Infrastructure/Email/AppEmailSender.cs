using System.Net;
using System.Net.Mail;
using GymManagement.BLL.Services.Interfaces;
using Microsoft.Extensions.Options;

namespace GymManagement.Infrastructure.Email;

/// <summary>
/// Uses SMTP when configured. In Development, unconfigured SMTP falls back to an
/// in-memory mailbox. In other environments a missing configuration is logged as
/// an error instead of leaking confirmation links or storing sensitive messages.
/// </summary>
public sealed class AppEmailSender : IAppEmailSender
{
    private readonly EmailOptions _options;
    private readonly DevelopmentMailbox _mailbox;
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<AppEmailSender> _logger;

    public AppEmailSender(
        IOptions<EmailOptions> options,
        DevelopmentMailbox mailbox,
        IWebHostEnvironment environment,
        ILogger<AppEmailSender> logger)
    {
        _options = options.Value;
        _mailbox = mailbox;
        _environment = environment;
        _logger = logger;
    }

    public async Task SendAsync(
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
                return;
            }

            _logger.LogError(
                "Email delivery is not configured. Set Email__Smtp__Host (and credentials) " +
                "to enable confirmation and password-reset emails.");
            return;
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
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send account email to {Email}.", toEmail);
        }
    }
}
