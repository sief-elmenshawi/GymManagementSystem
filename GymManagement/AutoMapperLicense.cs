using System.Text.RegularExpressions;
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

    private static readonly Regex JwtSegment =
        new(@"^[A-Za-z0-9_-]+={0,2}$", RegexOptions.Compiled | RegexOptions.CultureInvariant);

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
            if (!IsWellFormedKey(key))
            {
                throw new InvalidOperationException(
                    $"AutoMapper license key configured for {environmentName} environment is not a well-formed JWT. " +
                    $"Set {ConfigurationKey}, {AutoMapperEnvironmentVariable}, or {LuckyPennyEnvironmentVariable} to a valid key.");
            }

            return key;
        }

        throw new InvalidOperationException(
            $"AutoMapper license key is missing in {environmentName} environment. " +
            $"Set {ConfigurationKey}, {AutoMapperEnvironmentVariable}, or {LuckyPennyEnvironmentVariable} to a valid key.");
    }

    /// <summary>
    /// Checks the JWT shape (header.payload.signature) before handing the key to AutoMapper.
    /// Signature validation remains AutoMapper's responsibility.
    /// </summary>
    internal static bool IsWellFormedKey(string key)
    {
        var segments = key.Split('.');
        return segments.Length == 3
            && JwtSegment.IsMatch(segments[0])
            && JwtSegment.IsMatch(segments[1])
            && (segments[2].Length == 0 || JwtSegment.IsMatch(segments[2]));
    }

    public static bool IsConfigured(IConfiguration configuration) => ResolveKey(configuration) is not null;
}
