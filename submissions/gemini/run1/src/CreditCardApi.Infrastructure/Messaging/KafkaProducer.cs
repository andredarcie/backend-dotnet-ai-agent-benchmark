using System;
using System.Threading;
using System.Threading.Tasks;
using Confluent.Kafka;
using CreditCardApi.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CreditCardApi.Infrastructure.Messaging;

/// <summary>
/// A durable, idempotent Kafka producer implementation.
/// </summary>
public class KafkaProducer : IKafkaProducer, IDisposable
{
    private readonly IProducer<string, string> _producer;
    private readonly ILogger<KafkaProducer> _logger;

    public KafkaProducer(IConfiguration configuration, ILogger<KafkaProducer> logger)
    {
        _logger = logger;
        var bootstrapServers = configuration["Kafka:BootstrapServers"] 
                               ?? configuration["Kafka__BootstrapServers"] 
                               ?? "kafka:9092";

        var config = new ProducerConfig
        {
            BootstrapServers = bootstrapServers,
            Acks = Acks.All,
            EnableIdempotence = true, // Guarantees ordering and idempotent delivery
            MessageSendMaxRetries = 5,
            RetryBackoffMs = 1000,
            LingerMs = 5, // Improve throughput under load
            ClientId = "CreditCardApi-Producer"
        };

        _producer = new ProducerBuilder<string, string>(config).Build();
        _logger.LogInformation("Kafka durable producer initialized with broker: {Broker}", bootstrapServers);
    }

    public async Task PublishAsync(string topic, string key, string value, CancellationToken cancellationToken = default)
    {
        try
        {
            var message = new Message<string, string>
            {
                Key = key,
                Value = value,
                Timestamp = new Timestamp(DateTime.UtcNow)
            };

            // Using SendAsync with retry handled by Confluent.Kafka and outer resilience policies
            var deliveryResult = await _producer.ProduceAsync(topic, message, cancellationToken);
            _logger.LogInformation("Published message to {Topic} [Partition: {Partition}, Offset: {Offset}]", 
                topic, deliveryResult.Partition.Value, deliveryResult.Offset.Value);
        }
        catch (ProduceException<string, string> ex)
        {
            _logger.LogError(ex, "Error producing message with key {Key} to topic {Topic}: {Reason}", 
                key, topic, ex.Error.Reason);
            throw;
        }
    }

    public void Dispose()
    {
        // Flush pending messages and dispose
        _producer.Flush(TimeSpan.FromSeconds(5));
        _producer.Dispose();
        GC.SuppressFinalize(this);
    }
}
