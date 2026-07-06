using System.Diagnostics;
using System.Globalization;
using System.Text.Json;
using CreditCardApi.Application.Abstractions;
using CreditCardApi.Application.Transactions;
using CreditCardApi.Infrastructure.Observability;
using CreditCardApi.Infrastructure.Persistence;
using Microsoft.Extensions.Options;

namespace CreditCardApi.Infrastructure.Messaging.Outbox;

/// <summary>
/// Transactional-outbox implementation of <see cref="ITransactionEventPublisher"/>: the event is
/// staged as an <see cref="OutboxMessage"/> in the caller's unit of work, so it commits (or rolls
/// back) atomically with the transaction row itself.
/// </summary>
internal sealed class OutboxTransactionEventPublisher : ITransactionEventPublisher
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    private readonly CreditCardDbContext _db;
    private readonly KafkaOptions _options;
    private readonly TimeProvider _clock;

    public OutboxTransactionEventPublisher(
        CreditCardDbContext db,
        IOptions<KafkaOptions> options,
        TimeProvider clock)
    {
        _db = db;
        _options = options.Value;
        _clock = clock;
    }

    public void EnqueueTransactionCreated(TransactionResponse transaction)
    {
        _db.OutboxMessages.Add(new OutboxMessage
        {
            Topic = _options.TransactionsTopic,
            Key = transaction.Id.ToString(CultureInfo.InvariantCulture),
            Payload = JsonSerializer.Serialize(transaction, SerializerOptions),
            CorrelationId = Activity.Current?.GetBaggageItem(Correlation.BaggageKey),
            CreatedAt = _clock.GetUtcNow().UtcDateTime,
        });
    }
}
