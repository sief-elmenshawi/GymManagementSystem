using Microsoft.Extensions.Configuration;

namespace GymManagementPL;

/// <summary>
/// Resolves the AutoMapper license key from configuration or from the environment variables
/// documented by AutoMapper. Non-development environments require a key and fail fast at startup.
/// </summary>
public static class AutoMapperLicense
{
    public const string ConfigurationKey = "AutoMapper:LicenseKey";
    public const string AutoMapperEnvironmentVariable = "AUTOMAPPER_LICENSE_KEY";
    public const string LuckyPennyEnvironmentVariable = "LUCKYPENNY_LICENSE_KEY";

    /// <summary>
    /// Returns the first non-empty key, or <see langword="null"/> when none is configured.
    /// </summary>
    public static string? ResolveKey(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var candidates = new[]
        {
            configuration[ConfigurationKey],
            Environment.GetEnvironmentVariable(AutoMapperEnvironmentVariable),
            Environment.GetEnvironmentVariable(LuckyPennyEnvironmentVariable),
        };

        return candidates
            .Select(key => key?.Trim())
            .FirstOrDefault(key => !string.IsNullOrEmpty(key));
    }

    /// <summary>
    /// Returns a configured key or throws a clear startup exception when none exists.
    /// </summary>
    public static string RequireKey(IConfiguration configuration, string environmentName)
    {
        var key = ResolveKey(configuration);
        if (key is not null)
        {
            return key;
        }

        throw new InvalidOperationException(
            $"AutoMapper license key is missing in {environmentName} environment. " +
            $"Set {ConfigurationKey}, {AutoMapperEnvironmentVariable}, or {LuckyPennyEnvironmentVariable} to a valid key.");
    }

    public static bool IsConfigured(IConfiguration configuration) => ResolveKey(configuration) is not null;
}
