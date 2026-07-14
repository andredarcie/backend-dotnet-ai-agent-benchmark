namespace CreditCardApi.Infrastructure.Messaging;

public sealed class KafkaOptions
{
    public const string SectionName = "Kafka";

    public string BootstrapServers { get; init; } = "kafka:9092";

    public string TransactionsTopic { get; init; } = "transactions";
}
