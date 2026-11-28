using System.Runtime.CompilerServices;
using Xunit;

namespace GymManagement.Tests.Support;

/// <summary>
/// Opt-in browser test: needs a SQL test database, an explicit flag, and installed
/// Chromium binaries. Missing prerequisites produce a visible skip reason instead
/// of a failing test run.
/// </summary>
internal sealed class BrowserFactAttribute : FactAttribute
{
    public BrowserFactAttribute(
        [CallerFilePath] string? sourceFilePath = null,
        [CallerLineNumber] int sourceLineNumber = -1)
        : base(sourceFilePath, sourceLineNumber)
    {
        var reasons = new List<string>();

        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("GYM_TEST_CONNECTION_STRING")))
        {
            reasons.Add("set GYM_TEST_CONNECTION_STRING");
        }

        if (!string.Equals(
                Environment.GetEnvironmentVariable("GYM_RUN_BROWSER_TESTS"),
                "1",
                StringComparison.OrdinalIgnoreCase))
        {
            reasons.Add("set GYM_RUN_BROWSER_TESTS=1");
        }

        if (!IsChromiumInstalled())
        {
            reasons.Add("install Chromium (see README 'Browser-driven UI tests')");
        }

        if (reasons.Count > 0)
        {
            Skip = "Browser UI test skipped: " + string.Join("; ", reasons) + ".";
        }
    }

    private static bool IsChromiumInstalled()
    {
        var configuredPath = Environment.GetEnvironmentVariable("PLAYWRIGHT_BROWSERS_PATH");

        IEnumerable<string> roots = !string.IsNullOrWhiteSpace(configuredPath) &&
                                    configuredPath != "0"
            ? new[] { configuredPath }
            : DefaultRoots();

        return roots.Any(root =>
            Directory.Exists(root) &&
            Directory.EnumerateDirectories(root).Any(directory =>
                Path.GetFileName(directory).StartsWith("chromium-", StringComparison.OrdinalIgnoreCase)));
    }

    private static IEnumerable<string> DefaultRoots()
    {
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        yield return Path.Combine(home, ".cache", "ms-playwright");
        yield return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ms-playwright");
    }
}
