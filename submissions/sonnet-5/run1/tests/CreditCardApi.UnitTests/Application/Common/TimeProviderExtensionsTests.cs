using CreditCardApi.Application.Common;
using CreditCardApi.UnitTests.TestDoubles;

namespace CreditCardApi.UnitTests.Application.Common;

public class TimeProviderExtensionsTests
{
    [Fact]
    public void GetUtcNowTruncatedToMicroseconds_DropsSubMicrosecondTicks()
    {
        // 5 ticks (500ns) below a whole microsecond boundary.
        var now = new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero).AddTicks(TimeSpan.TicksPerMicrosecond * 3 + 5);
        var provider = new FixedTimeProvider(now);

        var truncated = provider.GetUtcNowTruncatedToMicroseconds();

        Assert.Equal(0, truncated.Ticks % TimeSpan.TicksPerMicrosecond);
        Assert.Equal(now.UtcDateTime.AddTicks(-5), truncated);
    }

    [Fact]
    public void GetUtcNowTruncatedToMicroseconds_IsIdempotentOnAnAlreadyTruncatedValue()
    {
        var now = new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);
        var provider = new FixedTimeProvider(now);

        Assert.Equal(now.UtcDateTime, provider.GetUtcNowTruncatedToMicroseconds());
    }
}
