using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using GymManagement.Tests.Support;
using Microsoft.Playwright;
using Xunit;

namespace GymManagement.Tests.EndToEnd;

/// <summary>
/// Real Chromium coverage on top of the existing HTTP E2E tests. Opt-in via
/// GYM_RUN_BROWSER_TESTS=1 after installing Chromium; see README for commands.
/// </summary>
public sealed class BrowserUiFlowTests
{
    private const string Password = "Test@12345";

    [BrowserFact]
    public async Task MemberRegistersConfirmsAndChangesEmailThroughRenderedPages()
    {
        var app = await StartAsync();
        try
        {
            var page = await app.NewPageAsync();
            var email = NewEmail("browser.register");
            var newEmail = NewEmail("browser.updated");

            await RegisterAsync(page, app.BaseUrl, email);
            await ConfirmAsync(page, app, email);
            await LoginAsync(page, app.BaseUrl, email, Password);

            await page.GotoAsync($"{app.BaseUrl}/Member/EditProfile");
            Assert.Contains("Edit my profile", await page.ContentAsync(), StringComparison.OrdinalIgnoreCase);

            await page.Locator("input[name='Email']").FillAsync(newEmail);
            await page.Locator("input[name='Phone']").FillAsync("01012345678");
            await page.Locator("input[name='BuildingNumber']").FillAsync("9");
            await page.Locator("input[name='City']").FillAsync("Cairo");
            await page.Locator("input[name='Street']").FillAsync("Browser Street");
            await page.Locator("input[name='CurrentPassword']").FillAsync(Password);
            await page.GetByRole(AriaRole.Button, new() { Name = "Save changes" }).ClickAsync();

            await page.WaitForURLAsync("**/Member");
            Assert.Contains(
                "Confirm your new email address",
                await page.ContentAsync(),
                StringComparison.OrdinalIgnoreCase);

            await ConfirmAsync(page, app, newEmail);
            await LoginAsync(page, app.BaseUrl, newEmail, Password);

            var portal = await page.ContentAsync();
            Assert.Contains(newEmail, portal, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            await app.DisposeAsync();
        }
    }

    [BrowserFact]
    public async Task LoginWithInvalidPassword_ShowsErrorInRenderedPage()
    {
        var app = await StartAsync();
        try
        {
            var page = await app.NewPageAsync();
            await page.GotoAsync($"{app.BaseUrl}/Account/Login");
            await page.Locator("input[name='Email']").FillAsync($"missing-{Guid.NewGuid():N}@example.com");
            await page.Locator("input[name='Password']").FillAsync("Wrong@12345");
            await page.Locator("button[type='submit']").ClickAsync();

            Assert.Contains(
                "Invalid email or password",
                await page.ContentAsync(),
                StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            await app.DisposeAsync();
        }
    }

    [BrowserFact]
    public async Task LoginWithUnconfirmedEmail_ShowsConfirmationMessageOnce()
    {
        const string message =
            "Please confirm your email address before signing in. Check your inbox for the confirmation link.";

        var app = await StartAsync();
        try
        {
            var page = await app.NewPageAsync();
            var email = NewEmail("browser.unconfirmed");

            // Register stops before confirmation, leaving EmailConfirmed = false.
            await RegisterAsync(page, app.BaseUrl, email);

            await page.GotoAsync($"{app.BaseUrl}/Account/Login");
            await page.Locator("input[name='Email']").FillAsync(email);
            await page.Locator("input[name='Password']").FillAsync(Password);
            await page.Locator("button[type='submit']").ClickAsync();

            var error = page.GetByText(message, new() { Exact = true });
            await error.First.WaitForAsync();
            Assert.Equal(1, await error.CountAsync());
        }
        finally
        {
            await app.DisposeAsync();
        }
    }

    private static async Task<BrowserTestApp> StartAsync()
    {
        var connectionString = Environment.GetEnvironmentVariable("GYM_TEST_CONNECTION_STRING")
            ?? throw new InvalidOperationException("Set GYM_TEST_CONNECTION_STRING for browser tests.");

        var app = new BrowserTestApp(connectionString, $"GymManagement_E2E_Browser_{Guid.NewGuid():N}");
        await app.StartAsync();
        return app;
    }

    private static async Task RegisterAsync(IPage page, string baseUrl, string email)
    {
        await page.GotoAsync($"{baseUrl}/Account/Register");
        await page.Locator("input[name='FirstName']").FillAsync("Browser");
        await page.Locator("input[name='LastName']").FillAsync("Member");
        await page.Locator("input[name='Email']").FillAsync(email);
        await page.Locator("input[name='Phone']").FillAsync("01012345678");
        await page.Locator("input[name='DateOfBirth']").FillAsync("1993-06-12");
        await page.Locator("select[name='Gender']").SelectOptionAsync("1");
        await page.Locator("input[name='Street']").FillAsync("Browser Street");
        await page.Locator("input[name='City']").FillAsync("Cairo");
        await page.Locator("input[name='BuildingNumber']").FillAsync("7");
        await page.Locator("input[name='Password']").FillAsync(Password);
        await page.Locator("input[name='ConfirmPassword']").FillAsync(Password);
        await page.Locator("button[type='submit']").ClickAsync();
        await page.WaitForURLAsync("**/Account/Login");
    }

    private static async Task LoginAsync(IPage page, string baseUrl, string email, string password)
    {
        await page.GotoAsync($"{baseUrl}/Account/Login");
        await page.Locator("input[name='Email']").FillAsync(email);
        await page.Locator("input[name='Password']").FillAsync(password);
        await page.Locator("button[type='submit']").ClickAsync();
        // Home redirects member-only accounts to /Member; staff stay on /Home.
        await page.WaitForURLAsync(new Regex("/(Home|Member)(/|$)"));
    }

    private static async Task ConfirmAsync(IPage page, BrowserTestApp app, string email)
    {
        await using var response = await page.Context.APIRequest.GetAsync(
            $"{app.BaseUrl}/dev-mailbox/{Uri.EscapeDataString(email)}");
        using var document = JsonDocument.Parse(await response.TextAsync());

        foreach (var item in document.RootElement.EnumerateArray())
        {
            var body = item.TryGetProperty("htmlBody", out var property)
                ? property.GetString()
                : null;
            if (string.IsNullOrEmpty(body))
            {
                continue;
            }

            var match = Regex.Match(
                body,
                "href=\"([^\"]*Account/ConfirmEmail[^\"]*)\"",
                RegexOptions.IgnoreCase);
            if (!match.Success) continue;

            await page.GotoAsync(WebUtility.HtmlDecode(match.Groups[1].Value));
            Assert.Contains("Email confirmed", await page.ContentAsync(), StringComparison.OrdinalIgnoreCase);
            return;
        }

        throw new InvalidOperationException($"No confirmation link was found for {email}.");
    }

    private static string NewEmail(string prefix) => $"{prefix}-{Guid.NewGuid():N}@example.com";
}
