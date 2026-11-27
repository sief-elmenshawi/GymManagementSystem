using Microsoft.Extensions.Configuration;

namespace GymManagementPL;

/// <summary>
/// Resolves the default-user seed password from configuration or from the environment.
/// No password is stored in source control.
/// </summary>
public static class SeedCredentials
{
    public const string ConfigurationKey = "SeedUsers:Password";
    public const string EnvironmentVariable = "GYM_SEED_PASSWORD";

    public static string? ResolvePassword(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        // An explicitly supplied environment value must be able to override local config.
        var fromEnvironment = Environment.GetEnvironmentVariable(EnvironmentVariable)?.Trim();
        if (!string.IsNullOrEmpty(fromEnvironment))
        {
            return fromEnvironment;
        }

        var configured = configuration[ConfigurationKey]?.Trim();
        return string.IsNullOrEmpty(configured) ? null : configured;
    }
}
