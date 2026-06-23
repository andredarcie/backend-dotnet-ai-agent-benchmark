using Confluent.Kafka;
using System.Text.Json;

namespace CreditCardApi.Services;

public interface IKafkaProducerService
{
    Task PublishTransactionAsync(object transaction);
}

public class KafkaProducerService(IConfiguration configuration) : IKafkaProducerService
{
    private readonly string _bootstrapServers = configuration["Kafka:BootstrapServers"] ?? "kafka:9092";

    public async Task PublishTransactionAsync(object transaction)
    {
        var config = new ProducerConfig { BootstrapServers = _bootstrapServers };

        using var producer = new ProducerBuilder<string, string>(config).Build();

        var transactionJson = JsonSerializer.Serialize(transaction);
        var transactionId = ((dynamic)transaction).Id.ToString();

        var message = new Message<string, string>
        {
            Key = transactionId,
            Value = transactionJson
        };

        var result = await producer.ProduceAsync("transactions", message);
        producer.Flush();
    }
}
