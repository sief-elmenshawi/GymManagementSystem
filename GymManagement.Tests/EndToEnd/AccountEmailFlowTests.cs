using System.Net;
using GymManagement.DAL.Data.DbContexts;
using GymManagement.Infrastructure.Email;
using GymManagement.Tests.Support;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace GymManagement.Tests.EndToEnd;

/// <summary>
/// Real HTTP coverage for email confirmation and password recovery.
/// Requires GYM_TEST_CONNECTION_STRING; otherwise these tests are reported as skipped.
/// </summary>
public sealed class AccountEmailFlowTests : IAsyncLifetime
{
    private const string Password = "Test@12345";
    private const string NewPassword = "Changed@12345";

    private readonly string? _connectionString =
        Environment.GetEnvironmentVariable("GYM_TEST_CONNECTION_STRING");
    private readonly string _databaseName = $"GymManagement_E2E_Email_{Guid.NewGuid():N}";

    private GymTestAppFactory? _factory;

    public async ValueTask InitializeAsync()
    {
        if (_connectionString is null) return;

        await TestDatabase.DropAsync(_connectionString, _databaseName);
        _factory = new GymTestAppFactory(_connectionString, _databaseName, Password);
        using var startupClient = _factory.CreateClient();
    }

    public async ValueTask DisposeAsync()
    {
        _factory?.Dispose();

        if (_connectionString is not null)
        {
            await TestDatabase.DropAsync(_connectionString, _databaseName);
        }
    }

    [SqlServerFact]
    public async Task ForgotPassword_ResetsPasswordAndAllowsLogin()
    {
        var factory = RequireFactory();
        var email = NewEmail();

        using var browser = CreateBrowser(factory);
        await AccountTestFlows.RegisterAsync(browser, email, NewPhone(), Password);
        await AccountTestFlows.ConfirmEmailAsync(browser, email);

        var forgotPage = await browser.GetStringAsync("/Account/ForgotPassword");
        Assert.Contains("Forgot your password?", forgotPage, StringComparison.OrdinalIgnoreCase);

        using var forgotResponse = await browser.PostFormAsync(
            "/Account/ForgotPassword",
            new Dictionary<string, string>
            {
                ["Email"] = email,
                ["__RequestVerificationToken"] = BrowserClient.ExtractToken(forgotPage),
            });
        AccountTestFlows.AssertRedirect(forgotResponse, "/Account/ForgotPasswordConfirmation");

        var confirmationPage = await browser.GetStringAsync("/Account/ForgotPasswordConfirmation");
        Assert.Contains("If an account exists for that email", confirmationPage, StringComparison.OrdinalIgnoreCase);

        var resetLink = await AccountTestFlows.GetMailboxLinkAsync(
            browser,
            email,
            "Account/ResetPassword");
        var resetPage = await browser.GetStringAsync(resetLink);
        Assert.Contains("Choose a new password", resetPage, StringComparison.OrdinalIgnoreCase);

        using var resetResponse = await browser.PostFormAsync(
            "/Account/ResetPassword",
            new Dictionary<string, string>
            {
                ["Email"] = BrowserClient.ExtractInputValue(resetPage, "Email"),
                ["Token"] = BrowserClient.ExtractInputValue(resetPage, "Token"),
                ["Password"] = NewPassword,
                ["ConfirmPassword"] = NewPassword,
                ["__RequestVerificationToken"] = BrowserClient.ExtractToken(resetPage),
            });
        AccountTestFlows.AssertRedirect(resetResponse, "/Account/ResetPasswordConfirmation");

        using var loginBrowser = CreateBrowser(factory);
        await AssertLoginFailsAsync(loginBrowser, email, Password);
        await AccountTestFlows.LoginAsync(loginBrowser, email, NewPassword);
    }

    [SqlServerFact]
    public async Task ForgotPassword_UnknownEmail_UsesSameConfirmationAndSendsNothing()
    {
        var factory = RequireFactory();
        var email = $"missing-{Guid.NewGuid():N}@example.com";

        using var browser = CreateBrowser(factory);
        var forgotPage = await browser.GetStringAsync("/Account/ForgotPassword");
        using var response = await browser.PostFormAsync(
            "/Account/ForgotPassword",
            new Dictionary<string, string>
            {
                ["Email"] = email,
                ["__RequestVerificationToken"] = BrowserClient.ExtractToken(forgotPage),
            });

        AccountTestFlows.AssertRedirect(response, "/Account/ForgotPasswordConfirmation");
        var confirmationPage = await browser.GetStringAsync("/Account/ForgotPasswordConfirmation");
        Assert.Contains("If an account exists for that email", confirmationPage, StringComparison.OrdinalIgnoreCase);

        var mailbox = await browser.GetStringAsync($"/dev-mailbox/{Uri.EscapeDataString(email)}");
        Assert.Equal("[]", mailbox.Trim());

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<GymDbContext>();
        Assert.False(await db.Users.AnyAsync(user => user.Email == email));
    }

    [SqlServerFact]
    public async Task ResendEmailConfirmation_UnconfirmedEmail_SendsFreshLinkThatConfirmsAccount()
    {
        var factory = RequireFactory();
        var email = NewEmail();

        using var browser = CreateBrowser(factory);
        await AccountTestFlows.RegisterAsync(browser, email, NewPhone(), Password);

        // Remove the original link so the one used below can only come from the resend POST.
        factory.Services.GetRequiredService<DevelopmentMailbox>().Clear();

        var resendPage = await browser.GetStringAsync("/Account/ResendEmailConfirmation");
        using var response = await browser.PostFormAsync(
            "/Account/ResendEmailConfirmation",
            new Dictionary<string, string>
            {
                ["Email"] = email,
                ["__RequestVerificationToken"] = BrowserClient.ExtractToken(resendPage),
            });
        AccountTestFlows.AssertRedirect(response, "/Account/ResendEmailConfirmationConfirmation");

        var confirmationPage = await browser.GetStringAsync("/Account/ResendEmailConfirmationConfirmation");
        Assert.Contains("If an unconfirmed account exists", confirmationPage, StringComparison.OrdinalIgnoreCase);

        await AccountTestFlows.ConfirmEmailAsync(browser, email);

        using var loginBrowser = CreateBrowser(factory);
        await AccountTestFlows.LoginAsync(loginBrowser, email, Password);
    }

    [SqlServerFact]
    public async Task ResendEmailConfirmation_ConfirmedOrUnknownEmail_UsesSameResponseAndSendsNothing()
    {
        var factory = RequireFactory();
        var email = NewEmail();
        var unknownEmail = $"missing-{Guid.NewGuid():N}@example.com";

        using var browser = CreateBrowser(factory);
        await AccountTestFlows.RegisterAsync(browser, email, NewPhone(), Password);
        await AccountTestFlows.ConfirmEmailAsync(browser, email);

        var mailbox = factory.Services.GetRequiredService<DevelopmentMailbox>();
        mailbox.Clear();

        foreach (var address in new[] { email, unknownEmail })
        {
            var resendPage = await browser.GetStringAsync("/Account/ResendEmailConfirmation");
            using var response = await browser.PostFormAsync(
                "/Account/ResendEmailConfirmation",
                new Dictionary<string, string>
                {
                    ["Email"] = address,
                    ["__RequestVerificationToken"] = BrowserClient.ExtractToken(resendPage),
                });

            AccountTestFlows.AssertRedirect(response, "/Account/ResendEmailConfirmationConfirmation");
        }

        var confirmationPage = await browser.GetStringAsync("/Account/ResendEmailConfirmationConfirmation");
        Assert.Contains("If an unconfirmed account exists", confirmationPage, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(mailbox.GetByEmail(email));
        Assert.Empty(mailbox.GetByEmail(unknownEmail));
    }

    private static async Task AssertLoginFailsAsync(
        BrowserClient browser,
        string email,
        string password)
    {
        var loginPage = await browser.GetStringAsync("/Account/Login");
        using var response = await browser.PostFormAsync(
            "/Account/Login",
            new Dictionary<string, string>
            {
                ["Email"] = email,
                ["Password"] = password,
                ["RememberMe"] = "false",
                ["__RequestVerificationToken"] = BrowserClient.ExtractToken(loginPage),
            });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("Invalid email or password", body, StringComparison.OrdinalIgnoreCase);
    }

    private GymTestAppFactory RequireFactory() =>
        _factory ?? throw new InvalidOperationException(
            "Set GYM_TEST_CONNECTION_STRING to run SQL Server end-to-end tests.");

    private static BrowserClient CreateBrowser(GymTestAppFactory factory) =>
        new(factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
        }));

    private static string NewEmail() => $"recovery-{Guid.NewGuid():N}@example.com";

    private static string NewPhone() => $"010{Random.Shared.Next(10_000_000, 99_999_999)}";
}
