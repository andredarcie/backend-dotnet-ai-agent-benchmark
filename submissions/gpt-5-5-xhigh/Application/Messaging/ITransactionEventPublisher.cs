using CreditCardApi.Contracts.Transactions;

namespace CreditCardApi.Application.Messaging;

public interface ITransactionEventPublisher
{
    Task PublishCreatedAsync(TransactionResponse transaction, CancellationToken cancellationToken = default);
}
