using System.Globalization;
using System.Security.Claims;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace GymManagement.Infrastructure.RateLimiting;

/// <summary>
/// Rate limits for anonymous account endpoints. Identity lockout is per account;
/// this adds a per-client-IP, per-endpoint ceiling for credential and mailbox abuse.
/// </summary>
public sealed class AuthenticationRateLimitOptions
{
    public const string SectionName = "AuthenticationRateLimit";

    // Deliberately higher than the account lockout threshold (5) so legitimate users
    // are not blocked while still stopping automated flooding.
    public int PermitLimit { get; set; } = 8;

    public int WindowSeconds { get; set; } = 60;

    // Authenticated self-service endpoints (for example repeated email changes)
    // get a separate, more generous budget so anonymous abuse cannot consume it.
    public int AuthenticatedPermitLimit { get; set; } = 20;

    public int AuthenticatedWindowSeconds { get; set; } = 60;
}

public static class AuthenticationRateLimiting
{
    public const string PolicyName = "AuthenticationEndpoints";

    public const string AuthenticatedPolicyName = "AuthenticatedEndpoints";
    private const string LogCategory = "GymManagement.RateLimiting";

    public static IServiceCollection AddAuthenticationRateLimiting(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddRateLimiter(options =>
        {
            // Read configuration lazily: WebApplicationFactory adds test settings during
            // host build, after the service registrations are declared.
            var configured = configuration.GetSection(AuthenticationRateLimitOptions.SectionName)
                .Get<AuthenticationRateLimitOptions>() ?? new AuthenticationRateLimitOptions();

            var permitLimit = Math.Clamp(configured.PermitLimit, 1, 1000);
            var window = TimeSpan.FromSeconds(Math.Clamp(configured.WindowSeconds, 1, 3600));
            var authenticatedPermitLimit = Math.Clamp(configured.AuthenticatedPermitLimit, 1, 1000);
            var authenticatedWindow = TimeSpan.FromSeconds(
                Math.Clamp(configured.AuthenticatedWindowSeconds, 1, 3600));

            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            options.AddPolicy<string>(PolicyName, context =>
            {
                // Endpoint is part of the partition key so login attempts do not consume
                // the same budget as password-recovery requests.
                var partitionKey =
                    $"{context.Connection.RemoteIpAddress?.ToString() ?? "unknown"}:{GetEndpointKey(context)}";

                return RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey,
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = permitLimit,
                        Window = window,
                        QueueLimit = 0,
                        AutoReplenishment = true,
                    });
            });

            options.AddPolicy<string>(AuthenticatedPolicyName, context =>
            {
                // Use the stable user id rather than the email address so no PII is
                // kept in the limiter partition key. Anonymous callers fall back to IP.
                var userKey = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
                var partitionKey =
                    $"{userKey ?? context.Connection.RemoteIpAddress?.ToString() ?? "unknown"}:{GetEndpointKey(context)}";

                return RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey,
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = authenticatedPermitLimit,
                        Window = authenticatedWindow,
                        QueueLimit = 0,
                        AutoReplenishment = true,
                    });
            });

            options.OnRejected = (rejectionContext, _) =>
            {
                var httpContext = rejectionContext.HttpContext;
                if (rejectionContext.Lease.TryGetMetadata(
                        MetadataName.RetryAfter,
                        out var retryAfter) &&
                    retryAfter > TimeSpan.Zero)
                {
                    httpContext.Response.Headers.RetryAfter = Math.Ceiling(retryAfter.TotalSeconds)
                        .ToString(CultureInfo.InvariantCulture);
                }

                httpContext.RequestServices
                    .GetRequiredService<ILoggerFactory>()
                    .CreateLogger(LogCategory)
                    .LogWarning(
                        "Rate limit reached for {Path} from {ClientIp}.",
                        httpContext.Request.Path.Value,
                        httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown");

                return ValueTask.CompletedTask;
            };
        });

        return services;
    }

    private static string GetEndpointKey(HttpContext context)
    {
        // MVC routing is case-insensitive and ignores trailing slashes, so the raw
        // request path is not a stable partition key. Use the matched endpoint when
        // available; otherwise fall back to a normalized path.
        var endpointKey = context.GetEndpoint()?.DisplayName;
        return string.IsNullOrEmpty(endpointKey)
            ? context.Request.Path.Value?.TrimEnd('/').ToLowerInvariant() ?? "unknown"
            : endpointKey;
    }
}
