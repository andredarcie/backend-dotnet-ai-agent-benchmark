namespace CreditCardApi.Application.Common;

internal static class TimeProviderExtensions
{
    /// <summary>
    /// Current UTC time truncated to microseconds — PostgreSQL's <c>timestamptz</c> precision.
    /// Truncating up front keeps the value returned in the create/update response identical to
    /// the value later read back from the database.
    /// </summary>
    public static DateTime GetUtcNowForStorage(this TimeProvider clock)
    {
        var now = clock.GetUtcNow().UtcDateTime;
        return new DateTime(now.Ticks - (now.Ticks % TimeSpan.TicksPerMicrosecond), DateTimeKind.Utc);
    }
}
