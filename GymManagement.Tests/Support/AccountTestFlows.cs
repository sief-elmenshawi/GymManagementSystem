using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using Xunit;

namespace GymManagement.Tests.Support;

/// <summary>
/// Shared helpers for exercising confirmation, login, and mailbox flows over real HTTP.
/// </summary>
internal static class AccountTestFlows
{
    internal static async Task RegisterAsync(
        BrowserClient browser,
        string email,
        string phone,
        string password,
        string firstName = "Account",
        string lastName = "Member")
    {
        var registerPage = await browser.GetStringAsync("/Account/Register");
        using var response = await browser.PostFormAsync(
            "/Account/Register",
            new Dictionary<string, string>
            {
                ["FirstName"] = firstName,
                ["LastName"] = lastName,
                ["Email"] = email,
                ["Phone"] = phone,
                ["DateOfBirth"] = "1993-06-12",
                ["Gender"] = "1",
                ["Street"] = "Recovery Street",
                ["City"] = "Cairo",
                ["BuildingNumber"] = "7",
                ["Password"] = password,
                ["ConfirmPassword"] = password,
                ["__RequestVerificationToken"] = BrowserClient.ExtractToken(registerPage),
            });

        AssertRedirect(response, "/Account/Login");
    }

    internal static async Task ConfirmEmailAsync(BrowserClient browser, string email)
    {
        var link = await GetMailboxLinkAsync(browser, email, "Account/ConfirmEmail");

        using var response = await browser.GetAsync(link);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Email confirmed", body, StringComparison.OrdinalIgnoreCase);
    }

    internal static async Task<string> GetMailboxLinkAsync(
        BrowserClient browser,
        string email,
        string linkMarker)
    {
        var json = await browser.GetStringAsync($"/dev-mailbox/{Uri.EscapeDataString(email)}");
        using var document = JsonDocument.Parse(json);

        foreach (var item in document.RootElement.EnumerateArray())
        {
            var body = GetBody(item);
            if (string.IsNullOrEmpty(body) ||
                !body.Contains(linkMarker, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var pattern = $"href=\"([^\"]*{Regex.Escape(linkMarker)}[^\"]*)\"";
            var match = Regex.Match(body, pattern, RegexOptions.IgnoreCase);
            if (match.Success)
            {
                return WebUtility.HtmlDecode(match.Groups[1].Value);
            }
        }

        throw new InvalidOperationException(
            $"No {linkMarker} link was found in the development mailbox for {email}.");
    }

    internal static async Task LoginAsync(
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

        AssertRedirect(response, "/Home");
    }

    internal static void AssertRedirect(HttpResponseMessage response, string expectedPath)
    {
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        var location = response.Headers.Location?.OriginalString;
        Assert.False(string.IsNullOrEmpty(location), "Redirect response has no Location header.");
        Assert.Contains(expectedPath, location, StringComparison.OrdinalIgnoreCase);
    }

    private static string? GetBody(JsonElement item)
    {
        if (item.TryGetProperty("htmlBody", out var camelCase) ||
            item.TryGetProperty("HtmlBody", out camelCase))
        {
            return camelCase.GetString();
        }

        return null;
    }
}
