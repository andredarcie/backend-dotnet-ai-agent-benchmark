using CreditCardApi.DTOs;

namespace CreditCardApi.Messaging;

/// <summary>
/// Publishes domain events for created transactions. Invoked from the application
/// (use-case) layer after persistence succeeds.
/// </summary>
public interface ITransactionEventPublisher
{
    Task PublishTransactionCreatedAsync(TransactionResponse transaction, CancellationToken ct = default);
}
