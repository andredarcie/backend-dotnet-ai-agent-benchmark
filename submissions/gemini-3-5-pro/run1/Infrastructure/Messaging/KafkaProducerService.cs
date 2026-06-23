using System;
using System.Text.Json;
using System.Threading.Tasks;
using Confluent.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CreditCardApi.Infrastructure.Messaging
{
    public class KafkaProducerService : IKafkaProducerService, IDisposable
    {
        private readonly IProducer<Null, string>? _producer;
        private readonly ILogger<KafkaProducerService> _logger;

        public KafkaProducerService(IConfiguration configuration, ILogger<KafkaProducerService> logger)
        {
            _logger = logger;
            try
            {
                var bootstrapServers = configuration.GetValue<string>("Kafka:BootstrapServers") ?? "localhost:9092";
                var config = new ProducerConfig
                {
                    BootstrapServers = bootstrapServers,
                    MessageTimeoutMs = 5000,
                    RequestTimeoutMs = 5000
                };
                _producer = new ProducerBuilder<Null, string>(config).Build();
                _logger.LogInformation("Kafka Producer successfully initialized with bootstrap servers: {Servers}", bootstrapServers);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize Kafka Producer. Will run in degraded mode.");
            }
        }

        public async Task PublishTransactionCreatedAsync(string topic, object message)
        {
            if (_producer == null)
            {
                _logger.LogWarning("Kafka producer is not initialized. Skipping publishing.");
                return;
            }

            try
            {
                var messageJson = JsonSerializer.Serialize(message, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                var kafkaMessage = new Message<Null, string>
                {
                    Value = messageJson
                };

                var result = await _producer.ProduceAsync(topic, kafkaMessage);
                _logger.LogInformation("Successfully published message to topic {Topic} at offset {Offset}", topic, result.Offset);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error publishing message to Kafka topic {Topic}", topic);
            }
        }

        public void Dispose()
        {
            _producer?.Flush(TimeSpan.FromSeconds(2));
            _producer?.Dispose();
        }
    }
}
