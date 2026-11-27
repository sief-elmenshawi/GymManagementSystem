using Microsoft.Extensions.Configuration;

namespace GymManagementPL;

/// <summary>
/// Reads hosting settings before the application starts. WebApplication.Urls is only
/// populated by StartAsync, so it cannot be used to decide whether to register
/// HTTPS redirection middleware.
/// </summary>
public static class HostingConfiguration
{
    public static int? GetHttpsPort(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        // ASPNETCORE_HTTPS_PORT is mapped to "https_port"; keep "HTTPS_PORT" and the
        // legacy custom "HttpsPort" key for compatibility with manual configuration.
        foreach (var key in new[] { "https_port", "HTTPS_PORT", "HttpsPort" })
        {
            if (int.TryParse(configuration[key], out var port) && port > 0 && port <= 65535)
            {
                return port;
            }
        }

        return null;
    }

    public static bool ShouldUseHttpsRedirection(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        if (GetHttpsPort(configuration) is not null)
        {
            return true;
        }

        var urls = configuration["urls"];
        return !string.IsNullOrWhiteSpace(urls)
            && urls.Contains("https", StringComparison.OrdinalIgnoreCase);
    }
}
