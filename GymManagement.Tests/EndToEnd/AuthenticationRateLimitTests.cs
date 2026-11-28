using System.Net;
using GymManagement.Tests.Support;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace GymManagement.Tests.EndToEnd;

/// <summary>
/// Verifies that anonymous and authenticated POST endpoints enforce their configured limits.
/// Requires GYM_TEST_CONNECTION_STRING; otherwise these tests are reported as skipped.
/// </summary>
public sealed class AuthenticationRateLimitTests : IAsyncLifetime
{
    private const string Password = "Test@12345";

    private readonly string? _connectionString =
        Environment.GetEnvironmentVariable("GYM_TEST_CONNECTION_STRING");
    private readonly string _databaseName = $"GymManagement_E2E_RateLimit_{Guid.NewGuid():N}";

    private GymTestAppFactory? _factory;

    public async ValueTask InitializeAsync()
    {
        if (_connectionString is null) return;

        await TestDatabase.DropAsync(_connectionString, _databaseName);
        _factory = new GymTestAppFactory(
            _connectionString,
            _databaseName,
            Password,
            new Dictionary<string, string?>
            {
                ["AuthenticationRateLimit:PermitLimit"] = "2",
                ["AuthenticationRateLimit:WindowSeconds"] = "60",
                ["AuthenticationRateLimit:AuthenticatedPermitLimit"] = "1",
                ["AuthenticationRateLimit:AuthenticatedWindowSeconds"] = "60",
            });

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
    public async Task Login_ExceededLimit_Returns429WithRetryAfter()
    {
        var factory = RequireFactory();
        using var browser = CreateBrowser(factory);
        var email = $"unknown-{Guid.NewGuid():N}@example.com";

        for (var attempt = 1; attempt <= 2; attempt++)
        {
            using var response = await PostLoginAsync(browser, email);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        using var rejected = await PostLoginAsync(browser, email);
        Assert.Equal(HttpStatusCode.TooManyRequests, rejected.StatusCode);
        Assert.True(rejected.Headers.TryGetValues("Retry-After", out var retryAfter));
        Assert.NotEmpty(retryAfter!);
    }

    [SqlServerFact]
    public async Task Login_ExceededLimit_WithDifferentPathCasing_Returns429()
    {
        var factory = RequireFactory();
        using var browser = CreateBrowser(factory);
        var email = $"unknown-{Guid.NewGuid():N}@example.com";

        // Routing is case-insensitive, so "/account/login" is the same endpoint
        // as "/Account/Login" and must consume the same rate-limit budget.
        for (var attempt = 1; attempt <= 2; attempt++)
        {
            using var response = await PostLoginAsync(browser, email);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        using var rejectedByCase = await PostLoginAsync(browser, email, "/account/login");
        Assert.Equal(HttpStatusCode.TooManyRequests, rejectedByCase.StatusCode);

        using var rejectedBySlash = await PostLoginAsync(browser, email, "/Account/Login/");
        Assert.Equal(HttpStatusCode.TooManyRequests, rejectedBySlash.StatusCode);
    }

    [SqlServerFact]
    public async Task Register_ExceededLimit_Returns429WithRetryAfter()
    {
        var factory = RequireFactory();
        using var browser = CreateBrowser(factory);

        for (var attempt = 1; attempt <= 2; attempt++)
        {
            var email = $"rate.register.{attempt}-{Guid.NewGuid():N}@example.com";
            using var response = await PostRegisterAsync(browser, email);
            Assert.NotEqual(HttpStatusCode.TooManyRequests, response.StatusCode);
        }

        using var rejected = await PostRegisterAsync(
            browser, $"rate.register.rejected-{Guid.NewGuid():N}@example.com");
        Assert.Equal(HttpStatusCode.TooManyRequests, rejected.StatusCode);
        Assert.True(rejected.Headers.TryGetValues("Retry-After", out var retryAfter));
        Assert.NotEmpty(retryAfter!);
    }

    [SqlServerFact]
    public async Task RateLimit_IsPartitionedPerEndpoint()
    {
        var factory = RequireFactory();
        using var browser = CreateBrowser(factory);
        var email = $"unknown-{Guid.NewGuid():N}@example.com";

        for (var attempt = 1; attempt <= 2; attempt++)
        {
            using var response = await PostLoginAsync(browser, email);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        // The same client may still use a different account endpoint.
        var forgotPage = await browser.GetStringAsync("/Account/ForgotPassword");
        using var forgotResponse = await browser.PostFormAsync(
            "/Account/ForgotPassword",
            new Dictionary<string, string>
            {
                ["Email"] = email,
                ["__RequestVerificationToken"] = BrowserClient.ExtractToken(forgotPage),
            });
        AccountTestFlows.AssertRedirect(forgotResponse, "/Account/ForgotPasswordConfirmation");
    }

    [SqlServerFact]
    public async Task EditProfile_ExceededAuthenticatedLimit_Returns429()
    {
        var factory = RequireFactory();
        using var browser = CreateBrowser(factory);
        var email = $"rate.profile-{Guid.NewGuid():N}@example.com";

        await AccountTestFlows.RegisterAsync(browser, email, "01012345678", Password);
        await AccountTestFlows.ConfirmEmailAsync(browser, email);
        await AccountTestFlows.LoginAsync(browser, email, Password);

        using (var accepted = await PostProfileEditAsync(browser, email))
        {
            AccountTestFlows.AssertRedirect(accepted, "/Member");
        }

        // The authenticated policy is per user and is independent of the anonymous IP budget.
        using var rejected = await PostProfileEditAsync(browser, email);
        Assert.Equal(HttpStatusCode.TooManyRequests, rejected.StatusCode);
    }

    private static async Task<HttpResponseMessage> PostLoginAsync(
        BrowserClient browser,
        string email,
        string path = "/Account/Login")
    {
        var loginPage = await browser.GetStringAsync(path);
        return await browser.PostFormAsync(
            path,
            new Dictionary<string, string>
            {
                ["Email"] = email,
                ["Password"] = "Wrong@12345",
                ["RememberMe"] = "false",
                ["__RequestVerificationToken"] = BrowserClient.ExtractToken(loginPage),
            });
    }

    private static async Task<HttpResponseMessage> PostRegisterAsync(BrowserClient browser, string email)
    {
        var registerPage = await browser.GetStringAsync("/Account/Register");
        return await browser.PostFormAsync(
            "/Account/Register",
            new Dictionary<string, string>
            {
                ["FirstName"] = "Rate",
                ["LastName"] = "Limit",
                ["Email"] = email,
                ["Phone"] = "01012345678",
                ["DateOfBirth"] = "1993-06-12",
                ["Gender"] = "1",
                ["Street"] = "Recovery Street",
                ["City"] = "Cairo",
                ["BuildingNumber"] = "7",
                ["Password"] = Password,
                ["ConfirmPassword"] = Password,
                ["__RequestVerificationToken"] = BrowserClient.ExtractToken(registerPage),
            });
    }

    private static async Task<HttpResponseMessage> PostProfileEditAsync(
        BrowserClient browser,
        string email)
    {
        var editPage = await browser.GetStringAsync("/Member/EditProfile");
        return await browser.PostFormAsync(
            "/Member/EditProfile",
            new Dictionary<string, string>
            {
                ["Email"] = email,
                ["Phone"] = "01098765432",
                ["BuildingNumber"] = "5",
                ["City"] = "Cairo",
                ["Street"] = "Rate Limit Street",
                ["CurrentPassword"] = Password,
                ["__RequestVerificationToken"] = BrowserClient.ExtractToken(editPage),
            });
    }

    private static BrowserClient CreateBrowser(GymTestAppFactory factory) =>
        new(factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
        }));

    private GymTestAppFactory RequireFactory() =>
        _factory ?? throw new InvalidOperationException(
            "Set GYM_TEST_CONNECTION_STRING to run SQL Server end-to-end tests.");
}
