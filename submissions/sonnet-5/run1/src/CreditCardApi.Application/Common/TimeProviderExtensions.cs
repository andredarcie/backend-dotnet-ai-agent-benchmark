namespace CreditCardApi.Application.Common;

public static class TimeProviderExtensions
{
    /// <summary>
    /// Postgres <c>timestamptz</c> stores microsecond precision while .NET <see cref="DateTime"/> ticks
    /// are 100ns; truncating here keeps a just-created row's timestamp identical to what a later read
    /// of the same row returns.
    /// </summary>
    public static DateTime GetUtcNowTruncatedToMicroseconds(this TimeProvider timeProvider)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        return now.AddTicks(-(now.Ticks % TimeSpan.TicksPerMicrosecond));
    }
}
