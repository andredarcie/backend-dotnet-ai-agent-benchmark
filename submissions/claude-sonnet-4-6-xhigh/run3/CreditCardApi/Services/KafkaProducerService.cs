using Confluent.Kafka;
using CreditCardApi.Models;
using System.Text.Json;

namespace CreditCardApi.Services;

public class KafkaProducerService : IKafkaProducerService, IDisposable
{
    private readonly IProducer<string, string> _producer;
    private readonly ILogger<KafkaProducerService> _logger;
    private const string Topic = "transactions";

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public KafkaProducerService(IConfiguration configuration, ILogger<KafkaProducerService> logger)
    {
        _logger = logger;
        var bootstrapServers = configuration["Kafka__BootstrapServers"]
            ?? configuration["Kafka:BootstrapServers"]
            ?? "kafka:9092";

        var config = new ProducerConfig
        {
            BootstrapServers = bootstrapServers,
            MessageTimeoutMs = 10000,
            RetryBackoffMs = 1000,
            MessageSendMaxRetries = 5
        };

        _producer = new ProducerBuilder<string, string>(config).Build();
    }

    public async Task PublishTransactionAsync(Transaction transaction)
    {
        var payload = JsonSerializer.Serialize(new
        {
            transaction.Id,
            transaction.CreditCardId,
            transaction.Amount,
            transaction.Merchant,
            transaction.Category,
            transaction.CreatedAt
        }, SerializerOptions);

        try
        {
            await _producer.ProduceAsync(Topic, new Message<string, string>
            {
                Key = transaction.Id.ToString(),
                Value = payload
            });
            _logger.LogInformation("Published transaction {Id} to Kafka", transaction.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish transaction {Id} to Kafka", transaction.Id);
            throw;
        }
    }

    public void Dispose()
    {
        _producer?.Flush(TimeSpan.FromSeconds(10));
        _producer?.Dispose();
    }
}
