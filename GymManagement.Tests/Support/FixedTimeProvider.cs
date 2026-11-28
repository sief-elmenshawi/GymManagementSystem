namespace GymManagement.Tests.Support;

/// <summary>
/// Deterministic UTC clock for service tests. Avoids depending on the machine
/// timezone when domain rules compare session/membership dates.
/// </summary>
public sealed class FixedTimeProvider : TimeProvider
{
    private readonly DateTimeOffset _now;

    public FixedTimeProvider(DateTimeOffset now) => _now = now.ToUniversalTime();

    public override DateTimeOffset GetUtcNow() => _now;
}
