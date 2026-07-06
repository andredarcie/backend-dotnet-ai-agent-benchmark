namespace CreditCardApi.Infrastructure.Observability;

/// <summary>
/// Names used to propagate the request correlation id end to end: HTTP header on the way in and
/// out, activity baggage inside the process, and Kafka message header across the broker.
/// </summary>
public static class Correlation
{
    /// <summary>HTTP and Kafka header carrying the correlation id.</summary>
    public const string HeaderName = "X-Correlation-Id";

    /// <summary>Activity (distributed tracing) baggage key carrying the correlation id.</summary>
    public const string BaggageKey = "correlation.id";
}
