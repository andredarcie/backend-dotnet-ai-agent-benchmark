namespace CreditCardApi.UnitTests.TestDoubles;

/// <summary>A <see cref="TimeProvider"/> frozen at a known instant, for deterministic assertions.</summary>
internal sealed class FixedTimeProvider : TimeProvider
{
    private readonly DateTimeOffset _now;

    public FixedTimeProvider(DateTimeOffset now) => _now = now;

    public override DateTimeOffset GetUtcNow() => _now;
}
