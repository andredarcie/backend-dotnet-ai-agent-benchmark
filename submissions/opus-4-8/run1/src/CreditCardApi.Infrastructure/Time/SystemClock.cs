using CreditCardApi.Application.Abstractions;

namespace CreditCardApi.Infrastructure.Time;

/// <summary>The real system clock.</summary>
public sealed class SystemClock : IClock
{
    /// <inheritdoc />
    public DateTime UtcNow => DateTime.UtcNow;
}
