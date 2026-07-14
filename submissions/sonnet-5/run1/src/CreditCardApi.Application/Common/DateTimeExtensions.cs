namespace CreditCardApi.Application.Common;

public static class DateTimeExtensions
{
    /// <summary>
    /// Postgres <c>timestamptz</c> stores microsecond precision; .NET <see cref="DateTime"/> ticks
    /// are 100ns. Truncating server-set timestamps here means a value returned by a create response
    /// matches byte-for-byte what a later read of the same row returns.
    /// </summary>
    public static DateTime TruncateToMicroseconds(this DateTime value) =>
        value.AddTicks(-(value.Ticks % TimeSpan.TicksPerMicrosecond));
}
