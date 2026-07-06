using Confluent.Kafka;
using Microsoft.Extensions.Logging;

namespace CreditCardApi.Infrastructure.Messaging;

public class KafkaProducer(IProducer<string, string> producer, ILogger<KafkaProducer> logger) : IKafkaProducer
{
    public async Task PublishAsync(string topic, string key, string value, CancellationToken cancellationToken = default)
    {
        try
        {
            var message = new Message<string, string>
            {
                Key = key,
                Value = value
            };

            var result = await producer.ProduceAsync(topic, message, cancellationToken);

            logger.LogInformation(
                "Message published to topic {Topic} with key {Key} at offset {Offset} partition {Partition}",
                topic, key, result.Offset, result.Partition);
        }
        catch (ProduceException<string, string> ex)
        {
            logger.LogError(ex, "Failed to produce message to topic {Topic}", topic);
            throw;
        }
    }
}
