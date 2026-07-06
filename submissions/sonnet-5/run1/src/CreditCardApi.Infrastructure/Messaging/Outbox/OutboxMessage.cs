using CreditCardApi.Domain.Entities;

namespace CreditCardApi.Infrastructure.Messaging.Outbox;

/// <summary>
/// A "transaction created" event staged for Kafka delivery in the same database transaction as the
/// transaction row itself. The payload is built at dispatch time from the FK-linked <see cref="Transaction"/>
/// (whose id is not yet assigned when the event is staged — EF Core's own relationship fixup resolves it
/// as part of the very same <c>SaveChanges</c> call, no second round-trip needed).
/// </summary>
public class OutboxMessage
{
    private OutboxMessage()
    {
        // Required by EF Core for materialization.
    }

    public OutboxMessage(string topic, Transaction transaction, DateTime occurredAtUtc, string? correlationId)
    {
        Topic = topic;
        Transaction = transaction;
        OccurredAt = occurredAtUtc;
        CorrelationId = correlationId;
    }

    public long Id { get; private set; }

    public string Topic { get; private set; } = string.Empty;

    public int TransactionId { get; private set; }

    public Transaction? Transaction { get; private set; }

    public string? CorrelationId { get; private set; }

    public DateTime OccurredAt { get; private set; }

    public DateTime? ProcessedAt { get; private set; }

    public int AttemptCount { get; private set; }

    public string? LastError { get; private set; }

    public void MarkProcessed(DateTime processedAtUtc) => ProcessedAt = processedAtUtc;

    public void RecordFailedAttempt(string error)
    {
        AttemptCount++;
        LastError = error;
    }
}
