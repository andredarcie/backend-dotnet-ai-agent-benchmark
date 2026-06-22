using System.Text.Json;
using Confluent.Kafka;
using CreditCardApi.Application.Messaging;
using CreditCardApi.Contracts.Transactions;
using Microsoft.Extensions.Options;

namespace CreditCardApi.Infrastructure.Messaging;

public sealed class KafkaTransactionEventPublisher : ITransactionEventPublisher, IDisposable
{
    private const string Topic = "transactions";
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);
    private readonly IProducer<string, string> _producer;

    public KafkaTransactionEventPublisher(IOptions<KafkaOptions> options)
    {
        var configuration = new ProducerConfig
        {
            BootstrapServers = options.Value.BootstrapServers,
            Acks = Acks.All,
            EnableIdempotence = true,
            MessageSendMaxRetries = 10,
            RetryBackoffMs = 250
        };

        _producer = new ProducerBuilder<string, string>(configuration).Build();
    }

    public Task PublishCreatedAsync(
        TransactionResponse transaction,
        CancellationToken cancellationToken = default) =>
        _producer.ProduceAsync(
            Topic,
            new Message<string, string>
            {
                Key = transaction.Id.ToString(System.Globalization.CultureInfo.InvariantCulture),
                Value = JsonSerializer.Serialize(transaction, SerializerOptions)
            },
            cancellationToken);

    public void Dispose()
    {
        _producer.Flush(TimeSpan.FromSeconds(5));
        _producer.Dispose();
    }
}
