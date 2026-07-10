using Confluent.Kafka;
using Microsoft.Extensions.Logging;

namespace CreditCardApi.Infrastructure.Messaging;

public class KafkaProducer : IKafkaProducer, IAsyncDisposable
{
    private readonly IProducer<string, string> _producer;
    private readonly ILogger<KafkaProducer> _logger;

    public KafkaProducer(string bootstrapServers, ILogger<KafkaProducer> logger)
    {
        _logger = logger;
        var config = new ProducerConfig
        {
            BootstrapServers = bootstrapServers,
            Acks = Acks.All,
            MessageTimeoutMs = 10000,
            RequestTimeoutMs = 10000,
            CompressionType = CompressionType.Snappy
        };

        _producer = new ProducerBuilder<string, string>(config)
            .SetErrorHandler((_, error) =>
            {
                _logger.LogError("Kafka error: {Reason}", error.Reason);
            })
            .Build();
    }

    public async Task PublishAsync(string topic, string key, string value, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _producer.ProduceAsync(
                topic,
                new Message<string, string> { Key = key, Value = value },
                cancellationToken);

            _logger.LogInformation(
                "Published message to {Topic} with key {Key} at partition {Partition} offset {Offset}",
                result.Topic, result.Key, result.Partition, result.Offset);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Kafka publish cancelled for topic {Topic}", topic);
            throw;
        }
        catch (ProduceException<string, string> ex)
        {
            _logger.LogError(ex, "Failed to publish to {Topic}: {Error}", topic, ex.Error.Reason);
            throw;
        }
    }

    public async ValueTask DisposeAsync()
    {
        _producer?.Flush(TimeSpan.FromSeconds(10));
        _producer?.Dispose();
        await ValueTask.CompletedTask;
    }
}
