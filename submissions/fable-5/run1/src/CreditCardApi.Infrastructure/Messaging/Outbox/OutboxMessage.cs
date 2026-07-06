namespace CreditCardApi.Infrastructure.Messaging.Outbox;

/// <summary>
/// A transactional-outbox row: an integration event persisted atomically with the business data
/// that produced it, awaiting delivery to Kafka by the <see cref="OutboxDispatcher"/>.
/// </summary>
public class OutboxMessage
{
    /// <summary>Monotonically increasing id; doubles as the dispatch order.</summary>
    public long Id { get; set; }

    /// <summary>Destination Kafka topic.</summary>
    public string Topic { get; set; } = string.Empty;

    /// <summary>Kafka message key (the transaction id), so equal keys land on the same partition.</summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>Serialized JSON payload (camelCase), produced verbatim to the topic.</summary>
    public string Payload { get; set; } = string.Empty;

    /// <summary>Correlation id of the request that raised the event, propagated as a Kafka header.</summary>
    public string? CorrelationId { get; set; }

    /// <summary>UTC timestamp when the event was staged.</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>UTC timestamp when the event was acknowledged by the broker; null while pending.</summary>
    public DateTime? ProcessedAt { get; set; }

    /// <summary>Number of failed delivery attempts so far.</summary>
    public int Attempts { get; set; }

    /// <summary>Reason of the most recent delivery failure, for operability.</summary>
    public string? LastError { get; set; }
}
