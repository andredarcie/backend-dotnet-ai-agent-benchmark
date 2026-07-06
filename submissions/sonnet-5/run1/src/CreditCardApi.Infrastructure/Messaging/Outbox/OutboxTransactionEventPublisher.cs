using CreditCardApi.Application.Abstractions;
using CreditCardApi.Application.Common;
using CreditCardApi.Domain.Entities;
using CreditCardApi.Infrastructure.Persistence;
using Microsoft.Extensions.Options;

namespace CreditCardApi.Infrastructure.Messaging.Outbox;

public class OutboxTransactionEventPublisher(
    CreditCardDbContext dbContext,
    IOptions<KafkaOptions> options,
    ICorrelationIdProvider correlationIdProvider,
    TimeProvider timeProvider) : ITransactionEventPublisher
{
    public void Stage(Transaction transaction)
    {
        var message = new OutboxMessage(
            options.Value.TransactionsTopic,
            transaction,
            timeProvider.GetUtcNowTruncatedToMicroseconds(),
            correlationIdProvider.Current);

        dbContext.OutboxMessages.Add(message);
    }
}
