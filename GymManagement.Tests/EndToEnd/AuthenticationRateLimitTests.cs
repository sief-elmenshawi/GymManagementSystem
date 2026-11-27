using System.Net;
using GymManagement.Tests.Support;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace GymManagement.Tests.EndToEnd;

/// <summary>
/// Verifies that account POST endpoints enforce the configured per-IP, per-endpoint limit.
/// Requires GYM_TEST_CONNECTION_STRING; otherwise these tests are reported as skipped.
/// </summary>
public sealed class AuthenticationRateLimitTests : IAsyncLifetime
{
    private const string Password = "Test@12345";

    private readonly string? _connectionString =
        Environment.GetEnvironmentVariable("GYM_TEST_CONNECTION_STRING");
    private readonly string _databaseName = $"GymManagement_E2E_RateLimit_{Guid.NewGuid():N}";

    private GymTestAppFactory? _factory;

    public async Task InitializeAsync()
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
            });

        using var startupClient = _factory.CreateClient();
    }

    public async Task DisposeAsync()
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

    private static async Task<HttpResponseMessage> PostLoginAsync(BrowserClient browser, string email)
    {
        var loginPage = await browser.GetStringAsync("/Account/Login");
        return await browser.PostFormAsync(
            "/Account/Login",
            new Dictionary<string, string>
            {
                ["Email"] = email,
                ["Password"] = "Wrong@12345",
                ["RememberMe"] = "false",
                ["__RequestVerificationToken"] = BrowserClient.ExtractToken(loginPage),
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
