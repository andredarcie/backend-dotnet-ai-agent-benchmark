namespace CreditCardApi.Infrastructure.Messaging.Consuming;

/// <summary>Idempotency ledger row: one per transaction id the consumer has already applied effects for.</summary>
public class ConsumedTransactionEvent
{
    private ConsumedTransactionEvent()
    {
        // Required by EF Core for materialization.
    }

    public ConsumedTransactionEvent(int transactionId, DateTime consumedAtUtc)
    {
        TransactionId = transactionId;
        ConsumedAt = consumedAtUtc;
    }

    public int TransactionId { get; private set; }

    public DateTime ConsumedAt { get; private set; }
}
