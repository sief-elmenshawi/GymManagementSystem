using GymManagement.Infrastructure.Email;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace GymManagement.Tests;

/// <summary>
/// Focused coverage for the SMTP failure-monitoring hook without requiring a real relay.
/// </summary>
public sealed class AppEmailSenderTests
{
    [Fact]
    public async Task MissingHost_InProduction_RecordsNotConfiguredFailureAndReturns()
    {
        var monitor = new RecordingMonitor();
        var sender = CreateSender(new EmailOptions(), "Production", monitor);

        var delivered = await sender.SendAsync("member@example.com", "Confirm your gym account", "<p>Hi</p>", TestContext.Current.CancellationToken);

        Assert.False(delivered);
        var failure = Assert.Single(monitor.Failures);
        Assert.Equal(EmailDeliveryFailureReason.NotConfigured, failure.Reason);
        Assert.Equal("member@example.com", failure.ToEmail);
        Assert.Null(failure.SmtpHost);
        Assert.Null(failure.Exception);
        Assert.Equal(0, monitor.SuccessCount);
    }

    [Fact]
    public async Task InvalidSenderAddress_RecordsSmtpFailureAndReturns()
    {
        var options = new EmailOptions
        {
            From = "not-an-address",
            Smtp = new SmtpOptions { Host = "localhost" },
        };
        var monitor = new RecordingMonitor();
        var sender = CreateSender(options, "Production", monitor);

        var delivered = await sender.SendAsync("member@example.com", "Reset your gym account password", "<p>Hi</p>", TestContext.Current.CancellationToken);

        Assert.False(delivered);
        var failure = Assert.Single(monitor.Failures);
        Assert.Equal(EmailDeliveryFailureReason.SmtpError, failure.Reason);
        Assert.Equal("localhost", failure.SmtpHost);
        Assert.NotNull(failure.Exception);
        Assert.Equal(0, monitor.SuccessCount);
    }

    [Fact]
    public async Task MissingHost_InDevelopment_UsesMailboxAndRecordsSuccess()
    {
        var mailbox = new DevelopmentMailbox();
        var monitor = new RecordingMonitor();
        var sender = CreateSender(new EmailOptions(), "Development", monitor, mailbox);

        var delivered = await sender.SendAsync("member@example.com", "Confirm your gym account", "<p>Link</p>", TestContext.Current.CancellationToken);

        Assert.True(delivered);
        Assert.Empty(monitor.Failures);
        Assert.Equal(1, monitor.SuccessCount);
        var message = Assert.Single(mailbox.GetByEmail("member@example.com"));
        Assert.Equal("Confirm your gym account", message.Subject);
    }

    private static AppEmailSender CreateSender(
        EmailOptions options,
        string environmentName,
        RecordingMonitor monitor,
        DevelopmentMailbox? mailbox = null)
    {
        var environment = new Mock<IWebHostEnvironment>();
        environment.SetupGet(item => item.EnvironmentName).Returns(environmentName);

        return new AppEmailSender(
            Options.Create(options),
            mailbox ?? new DevelopmentMailbox(),
            environment.Object,
            monitor);
    }

    private sealed class RecordingMonitor : IEmailDeliveryMonitor
    {
        public List<EmailDeliveryFailure> Failures { get; } = new();

        public int SuccessCount { get; private set; }

        public void RecordSuccess() => SuccessCount++;

        public void RecordFailure(EmailDeliveryFailure failure) => Failures.Add(failure);
    }
}
