using Confluent.Kafka;
using System.Text.Json;

namespace CreditCardApi.Infrastructure;

public class KafkaProducer : IKafkaProducer
{
    private readonly IProducer<string, string> _producer;
    private readonly ILogger<KafkaProducer> _logger;

    public KafkaProducer(string bootstrapServers, ILogger<KafkaProducer> logger)
    {
        _logger = logger;
        var config = new ProducerConfig
        {
            BootstrapServers = bootstrapServers,
            AllowAutoCreateTopics = true
        };
        _producer = new ProducerBuilder<string, string>(config)
            .SetErrorHandler((_, e) => _logger.LogError($"Kafka error: {e.Reason}"))
            .Build();
    }

    public async Task PublishTransactionAsync(string key, string message)
    {
        try
        {
            var result = await _producer.ProduceAsync("transactions", new Message<string, string>
            {
                Key = key,
                Value = message
            });
            _logger.LogInformation($"Message published to topic {result.Topic} at offset {result.Offset}");
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error publishing to Kafka: {ex.Message}");
            throw;
        }
    }
}
