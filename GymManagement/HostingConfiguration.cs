using System.Net;
using Microsoft.AspNetCore.HttpOverrides;
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

    /// <summary>
    /// Builds forwarded-header settings from configuration. Loopback proxies are trusted
    /// by ASP.NET Core by default; additional proxies/networks must be explicit so a
    /// client cannot spoof X-Forwarded-For to bypass rate limiting.
    /// </summary>
    public static ForwardedHeadersOptions GetForwardedHeadersOptions(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var options = new ForwardedHeadersOptions
        {
            ForwardedHeaders = ForwardedHeaders.XForwardedFor
                | ForwardedHeaders.XForwardedProto
                | ForwardedHeaders.XForwardedHost,
            ForwardLimit = 1,
        };

        if (int.TryParse(configuration["ForwardedHeaders:ForwardLimit"], out var forwardLimit)
            && forwardLimit is >= 1 and <= 10)
        {
            options.ForwardLimit = forwardLimit;
        }

        foreach (var value in configuration.GetSection("ForwardedHeaders:KnownProxies")
                     .Get<string[]>() ?? [])
        {
            if (!IPAddress.TryParse(value, out var address))
            {
                throw new InvalidOperationException(
                    "ForwardedHeaders:KnownProxies contains an invalid IP address. Value not shown.");
            }

            options.KnownProxies.Add(address);
        }

        foreach (var value in configuration.GetSection("ForwardedHeaders:KnownNetworks")
                     .Get<string[]>() ?? [])
        {
            if (!Microsoft.AspNetCore.HttpOverrides.IPNetwork.TryParse(value, out var network))
            {
                throw new InvalidOperationException(
                    "ForwardedHeaders:KnownNetworks contains an invalid CIDR network. Value not shown.");
            }

            options.KnownNetworks.Add(network);
        }

        return options;
    }

    /// <summary>
    /// Production must never rely on the wildcard host filter from the committed
    /// appsettings.json; otherwise host-header attacks can poison generated links.
    /// </summary>
    public static void RequireExplicitAllowedHosts(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var allowedHosts = configuration["AllowedHosts"];
        if (string.IsNullOrWhiteSpace(allowedHosts) || allowedHosts.Trim() == "*")
        {
            throw new InvalidOperationException(
                "AllowedHosts must be set to an explicit, semicolon-separated host list " +
                "in non-Development environments (for example: gym.example.com;www.gym.example.com).");
        }
    }
}
