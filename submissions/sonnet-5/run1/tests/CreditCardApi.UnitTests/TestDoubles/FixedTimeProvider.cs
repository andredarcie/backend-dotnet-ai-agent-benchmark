namespace CreditCardApi.UnitTests.TestDoubles;

/// <summary>A <see cref="TimeProvider"/> that always returns the same instant, for deterministic assertions.</summary>
public sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => now;
}
