namespace CreditCardApi.Infrastructure.Messaging.Consuming;

/// <summary>
/// Idempotency ledger for the <c>transactions</c> topic consumer: one row per transaction event
/// that has been processed. The primary key makes duplicate deliveries detectable, which is what
/// turns Kafka's at-least-once delivery into exactly-once processing.
/// </summary>
public class ConsumedTransactionEvent
{
    /// <summary>Id of the transaction the event refers to (the Kafka message key).</summary>
    public int TransactionId { get; set; }

    /// <summary>UTC timestamp when the event was consumed.</summary>
    public DateTime ConsumedAt { get; set; }
}
