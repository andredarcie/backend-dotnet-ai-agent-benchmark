using Confluent.Kafka;
using CreditCardApi.Infrastructure.Configuration;
using Microsoft.Extensions.Options;

namespace CreditCardApi.Infrastructure.Messaging;

public static class KafkaProducerFactory
{
    public static IProducer<string, string> Create(IServiceProvider serviceProvider)
    {
        var options = serviceProvider.GetRequiredService<IOptions<KafkaOptions>>().Value;
        var config = new ProducerConfig
        {
            BootstrapServers = options.BootstrapServers,
            Acks = Acks.All,
            EnableIdempotence = true,
            MessageSendMaxRetries = 10,
            RetryBackoffMs = 500,
            LingerMs = 5,
            SocketTimeoutMs = 10_000,
            RequestTimeoutMs = 10_000,
            MessageTimeoutMs = 30_000,
            ClientId = "credit-card-api"
        };

        return new ProducerBuilder<string, string>(config).Build();
    }
}
