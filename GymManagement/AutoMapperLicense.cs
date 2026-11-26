using Microsoft.Extensions.Configuration;

namespace GymManagementPL;

/// <summary>
/// Resolves the AutoMapper license key from configuration or from the environment variables
/// documented by AutoMapper. Resolving it here as well lets the host log a clear diagnostic
/// when a non-development environment starts without a key.
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

    public static bool IsConfigured(IConfiguration configuration) => ResolveKey(configuration) is not null;
}
