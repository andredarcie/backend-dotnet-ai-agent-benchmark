using Confluent.Kafka;

namespace CreditCardApi.Services;

public class KafkaProducer : IKafkaProducer
{
    private readonly IProducer<string, string> _producer;
    private readonly ILogger<KafkaProducer> _logger;

    public KafkaProducer(IConfiguration configuration, ILogger<KafkaProducer> logger)
    {
        _logger = logger;
        var bootstrapServers = configuration.GetValue<string>("Kafka:BootstrapServers") ?? "kafka:9092";

        var config = new ProducerConfig
        {
            BootstrapServers = bootstrapServers
        };

        _producer = new ProducerBuilder<string, string>(config).Build();
    }

    public async Task PublishTransactionAsync(string message, string key)
    {
        try
        {
            var result = await _producer.ProduceAsync("transactions", new Message<string, string>
            {
                Key = key,
                Value = message
            });
            _logger.LogInformation($"Published message to Kafka with key: {key}");
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error publishing to Kafka: {ex.Message}");
            throw;
        }
    }
}
