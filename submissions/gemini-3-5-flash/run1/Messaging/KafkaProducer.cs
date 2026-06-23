using System;
using System.Text.Json;
using System.Threading.Tasks;
using Confluent.Kafka;
using Gemini.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Gemini.Messaging;

public class KafkaProducer : IKafkaProducer, IDisposable
{
    private readonly IProducer<string, string> _producer;
    private readonly string _topic = "transactions";
    private readonly ILogger<KafkaProducer> _logger;

    public KafkaProducer(IConfiguration configuration, ILogger<KafkaProducer> logger)
    {
        _logger = logger;
        
        var bootstrapServers = configuration["Kafka:BootstrapServers"];
        if (string.IsNullOrWhiteSpace(bootstrapServers))
        {
            bootstrapServers = "kafka:9092";
        }

        var config = new ProducerConfig
        {
            BootstrapServers = bootstrapServers,
            ClientId = "gemini-api-producer",
            // Enable auto-creation of topics on the broker side, though the broker handles this by default.
            // Under load, this is resilient.
            Acks = Acks.All,
            MessageSendMaxRetries = 3
        };

        _logger.LogInformation("Initializing Kafka Producer with bootstrap servers: {Servers}", bootstrapServers);
        _producer = new ProducerBuilder<string, string>(config).Build();
    }

    public async Task PublishTransactionAsync(Transaction transaction)
    {
        try
        {
            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
            var jsonValue = JsonSerializer.Serialize(transaction, options);
            var key = transaction.Id.ToString();

            var message = new Message<string, string>
            {
                Key = key,
                Value = jsonValue
            };

            _logger.LogInformation("Publishing transaction {TransactionId} to topic '{Topic}'", key, _topic);
            
            var result = await _producer.ProduceAsync(_topic, message);
            _logger.LogInformation("Published transaction {TransactionId} to partition {Partition} at offset {Offset}", 
                key, result.Partition.Value, result.Offset.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish transaction {TransactionId} to Kafka", transaction.Id);
            throw;
        }
    }

    public void Dispose()
    {
        try
        {
            _producer.Flush(TimeSpan.FromSeconds(5));
            _producer.Dispose();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error disposing Kafka producer");
        }
    }
}
