using CreditCardApi.Application.Common;

namespace CreditCardApi.UnitTests.Common;

public class DateTimeExtensionsTests
{
    [Fact]
    public void TruncateToMicroseconds_DropsSubMicrosecondTicks()
    {
        // 1 tick = 100ns; 1 microsecond = 10 ticks. 7 extra ticks below a microsecond boundary.
        var value = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc).AddTicks(37);

        var truncated = value.TruncateToMicroseconds();

        Assert.Equal(0, truncated.Ticks % TimeSpan.TicksPerMicrosecond);
        Assert.Equal(30, truncated.Ticks - new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc).Ticks);
    }

    [Fact]
    public void TruncateToMicroseconds_WhenAlreadyMicrosecondAligned_IsUnchanged()
    {
        var value = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc).AddTicks(20);

        var truncated = value.TruncateToMicroseconds();

        Assert.Equal(value, truncated);
    }
}
