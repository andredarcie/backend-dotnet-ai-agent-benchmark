namespace CreditCardApi.Infrastructure.Persistence.Outbox;

/// <summary>
/// A pending integration event stored in the same database transaction as the business change
/// (Transactional Outbox). A background dispatcher publishes it to the broker and marks it processed.
/// </summary>
public sealed class OutboxMessage
{
    /// <summary>Stable identifier, also used as the broker idempotency anchor.</summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Destination topic.</summary>
    public string Topic { get; set; } = string.Empty;

    /// <summary>Partition key (the transaction id as a string).</summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>Logical event type, for diagnostics.</summary>
    public string EventType { get; set; } = string.Empty;

    /// <summary>The serialized (camelCase JSON) event payload.</summary>
    public string Payload { get; set; } = string.Empty;

    /// <summary>When the event was enqueued (UTC).</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>When the event was successfully published (UTC), or <c>null</c> while pending.</summary>
    public DateTime? ProcessedAt { get; set; }

    /// <summary>Number of delivery attempts made so far.</summary>
    public int Attempts { get; set; }

    /// <summary>Last error seen while trying to publish, if any.</summary>
    public string? LastError { get; set; }
}
