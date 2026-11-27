using System.Globalization;
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
}

public static class AuthenticationRateLimiting
{
    public const string PolicyName = "AuthenticationEndpoints";
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

            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            options.AddPolicy<string>(PolicyName, context =>
            {
                // Path is part of the partition key so login attempts do not consume
                // the same budget as password-recovery requests.
                var partitionKey =
                    $"{context.Connection.RemoteIpAddress?.ToString() ?? "unknown"}:{context.Request.Path.Value}";

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
}
