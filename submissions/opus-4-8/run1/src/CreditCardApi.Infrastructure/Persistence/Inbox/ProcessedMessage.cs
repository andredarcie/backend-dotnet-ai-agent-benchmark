namespace CreditCardApi.Infrastructure.Persistence.Inbox;

/// <summary>
/// Records the key of a Kafka message that has already been handled, so the consumer can skip
/// duplicates (idempotent processing) before committing the offset.
/// </summary>
public sealed class ProcessedMessage
{
    /// <summary>The idempotency key of the message (the transaction id).</summary>
    public string MessageKey { get; set; } = string.Empty;

    /// <summary>The topic the message came from.</summary>
    public string Topic { get; set; } = string.Empty;

    /// <summary>When the message was processed (UTC).</summary>
    public DateTime ProcessedAt { get; set; }
}
