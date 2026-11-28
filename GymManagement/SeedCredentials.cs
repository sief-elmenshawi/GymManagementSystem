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

    public const string EnabledConfigurationKey = "SeedUsers:Enabled";
    public const string EnabledEnvironmentVariable = "GYM_SEED_USERS_ENABLED";

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

    /// <summary>
    /// Development keeps the historical behavior so local runs and tests need no
    /// extra switch. Non-development environments must opt in explicitly because
    /// the seeded identities are predictable.
    /// </summary>
    public static bool ResolveUserSeedingEnabled(
        IConfiguration configuration,
        bool isDevelopment)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var fromEnvironment = Environment.GetEnvironmentVariable(EnabledEnvironmentVariable);
        if (bool.TryParse(fromEnvironment, out var envEnabled))
        {
            return envEnabled;
        }

        if (bool.TryParse(configuration[EnabledConfigurationKey], out var configured))
        {
            return configured;
        }

        return isDevelopment;
    }
}
