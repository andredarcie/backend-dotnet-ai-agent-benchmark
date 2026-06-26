using System.Text.Json;
using CreditCardApi.Application.Abstractions;
using CreditCardApi.Infrastructure.Persistence;
using CreditCardApi.Infrastructure.Persistence.Outbox;
using CreditCardApi.Infrastructure.Serialization;

namespace CreditCardApi.Infrastructure.Messaging;

/// <summary>
/// Writes integration events to the outbox table on the current context, so they are committed in
/// the same transaction as the business change and published later by <see cref="OutboxDispatcher"/>.
/// </summary>
public sealed class OutboxEventPublisher : IIntegrationEventPublisher
{
    private readonly AppDbContext _db;
    private readonly IClock _clock;

    /// <summary>Creates the publisher over the current unit-of-work context.</summary>
    public OutboxEventPublisher(AppDbContext db, IClock clock)
    {
        _db = db;
        _clock = clock;
    }

    /// <inheritdoc />
    public void Enqueue(string topic, string key, string eventType, object payload)
    {
        _db.OutboxMessages.Add(new OutboxMessage
        {
            Topic = topic,
            Key = key,
            EventType = eventType,
            Payload = JsonSerializer.Serialize(payload, JsonDefaults.CamelCase),
            CreatedAt = _clock.UtcNow,
        });
    }
}
