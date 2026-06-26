namespace CreditCardApi.Application.Abstractions;

/// <summary>
/// Stages an integration event for reliable delivery. Implementations write the event to the
/// outbox within the current unit of work, so it is committed atomically with the business change
/// and dispatched to the broker afterwards by a background processor.
/// </summary>
public interface IIntegrationEventPublisher
{
    /// <summary>
    /// Enqueues an event to be published to <paramref name="topic"/> with the given partition
    /// <paramref name="key"/>. The <paramref name="payload"/> is serialized to camelCase JSON.
    /// </summary>
    void Enqueue(string topic, string key, string eventType, object payload);
}
