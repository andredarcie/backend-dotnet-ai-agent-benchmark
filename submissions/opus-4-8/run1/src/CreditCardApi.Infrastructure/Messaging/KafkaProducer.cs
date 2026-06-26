using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CreditCardApi.Infrastructure.Messaging;

/// <summary>
/// A durable, idempotent Kafka producer. Configured with <c>acks=all</c> and idempotence so messages
/// are written to all in-sync replicas exactly once (per producer session), with automatic retries.
/// Registered as a singleton and reused for the life of the process.
/// </summary>
public sealed class KafkaProducer : IDisposable
{
    private readonly IProducer<string, string> _producer;
    private readonly ILogger<KafkaProducer> _logger;

    /// <summary>Builds the producer from <see cref="KafkaOptions"/>.</summary>
    public KafkaProducer(IOptions<KafkaOptions> options, ILogger<KafkaProducer> logger)
    {
        _logger = logger;
        var config = new ProducerConfig
        {
            BootstrapServers = options.Value.BootstrapServers,
            Acks = Acks.All,
            EnableIdempotence = true,
            MessageSendMaxRetries = 10,
            RetryBackoffMs = 200,
            LingerMs = 5,
            EnableDeliveryReports = true,
        };

        _producer = new ProducerBuilder<string, string>(config)
            .SetErrorHandler((_, error) =>
                _logger.LogError("Kafka producer error: {Code} {Reason}", error.Code, error.Reason))
            .Build();
    }

    /// <summary>Produces a keyed message and awaits broker acknowledgement.</summary>
    public Task<DeliveryResult<string, string>> ProduceAsync(
        string topic, string key, string value, CancellationToken cancellationToken) =>
        _producer.ProduceAsync(topic, new Message<string, string> { Key = key, Value = value }, cancellationToken);

    /// <inheritdoc />
    public void Dispose()
    {
        try
        {
            _producer.Flush(TimeSpan.FromSeconds(5));
        }
        catch (ObjectDisposedException)
        {
            // Already disposed; nothing to flush.
        }

        _producer.Dispose();
    }
}
