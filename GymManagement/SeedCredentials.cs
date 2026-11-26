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

        var configured = configuration[ConfigurationKey]?.Trim();
        if (!string.IsNullOrEmpty(configured))
        {
            return configured;
        }

        var fromEnvironment = Environment.GetEnvironmentVariable(EnvironmentVariable)?.Trim();
        return string.IsNullOrEmpty(fromEnvironment) ? null : fromEnvironment;
    }
}
