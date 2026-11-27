using System.Diagnostics.Metrics;

namespace GymManagement.Infrastructure.Email;

/// <summary>
/// Default monitor: structured logs for humans/log-based alerts and OpenTelemetry-compatible
/// counters for metric-based alerts. Tags are low-cardinality; addresses are never tag values.
/// </summary>
public sealed class EmailDeliveryMonitor : IEmailDeliveryMonitor, IDisposable
{
    public const string MeterName = "GymManagement.Email";
    public const string FailureCounterName = "email_delivery_failures";
    public const string SuccessCounterName = "email_delivery_successes";

    private readonly Meter _meter = new(MeterName, "1.0.0");
    private readonly Counter<long> _failures;
    private readonly Counter<long> _successes;
    private readonly ILogger<EmailDeliveryMonitor> _logger;

    public EmailDeliveryMonitor(ILogger<EmailDeliveryMonitor> logger)
    {
        _logger = logger;
        _failures = _meter.CreateCounter<long>(
            FailureCounterName,
            description: "Number of account emails that could not be delivered.");
        _successes = _meter.CreateCounter<long>(
            SuccessCounterName,
            description: "Number of account emails accepted for delivery.");
    }

    public void RecordSuccess()
    {
        _successes.Add(1);
    }

    public void RecordFailure(EmailDeliveryFailure failure)
    {
        ArgumentNullException.ThrowIfNull(failure);

        _failures.Add(1,
            new KeyValuePair<string, object?>("reason", failure.Reason.ToString()),
            new KeyValuePair<string, object?>("smtp_host", failure.SmtpHost ?? "none"));

        if (failure.Reason == EmailDeliveryFailureReason.NotConfigured)
        {
            _logger.LogError(
                "Email delivery is not configured. Set Email__Smtp__Host (and credentials) " +
                "to enable confirmation and password-reset emails. " +
                "Failed for {Email} (subject: {Subject}).",
                failure.ToEmail,
                failure.Subject);
            return;
        }

        _logger.LogError(
            failure.Exception,
            "Failed to send account email to {Email} (subject: {Subject}, smtp host: {SmtpHost}).",
            failure.ToEmail,
            failure.Subject,
            failure.SmtpHost ?? "<none>");
    }

    public void Dispose()
    {
        _meter.Dispose();
    }
}
