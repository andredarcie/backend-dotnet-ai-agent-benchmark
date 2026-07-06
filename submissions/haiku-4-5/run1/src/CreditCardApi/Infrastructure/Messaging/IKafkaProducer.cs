namespace CreditCardApi.Infrastructure.Messaging;

/// <summary>
/// Interface for publishing messages to Kafka.
/// </summary>
public interface IKafkaProducer
{
    /// <summary>
    /// Publishes a message to a Kafka topic.
    /// </summary>
    /// <param name="topic">The topic name.</param>
    /// <param name="key">The message key (for partitioning).</param>
    /// <param name="value">The message value as JSON.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task PublishAsync(string topic, string key, string value, CancellationToken cancellationToken = default);
}
