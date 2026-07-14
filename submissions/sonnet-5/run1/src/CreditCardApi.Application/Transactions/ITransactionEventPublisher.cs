using CreditCardApi.Application.Transactions.Dtos;

namespace CreditCardApi.Application.Transactions;

/// <summary>
/// Publishes the "transaction created" event to Kafka. A publish failure must never fail the
/// request that already persisted the row - implementations catch and log instead of throwing.
/// </summary>
public interface ITransactionEventPublisher
{
    Task PublishCreatedAsync(TransactionResponse transaction, CancellationToken cancellationToken);
}
