using CreditCardApi.Application.Transactions;

namespace CreditCardApi.Application.Abstractions;

/// <summary>
/// Records integration events about transactions for asynchronous delivery to the message broker.
/// </summary>
/// <remarks>
/// Implementations follow the transactional-outbox pattern: the event is staged in the same unit
/// of work as the business data, so both commit or roll back together, and a background dispatcher
/// delivers it to the broker afterwards.
/// </remarks>
public interface ITransactionEventPublisher
{
    /// <summary>Stages a "transaction created" event for the successfully created transaction.</summary>
    void EnqueueTransactionCreated(TransactionResponse transaction);
}
