using CreditCardApi.Domain.Entities;

namespace CreditCardApi.Application.Abstractions;

/// <summary>
/// Stages the "transaction created" event for delivery. The implementation writes it into the same
/// unit of work as the transaction itself (the transactional outbox pattern), so a crash between the
/// two writes is impossible and the event can never diverge from what was actually persisted.
/// </summary>
public interface ITransactionEventPublisher
{
    void Stage(Transaction transaction);
}
