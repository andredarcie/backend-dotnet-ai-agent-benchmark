namespace CreditCardApi.Infrastructure.Messaging;

public sealed class KafkaOptions
{
    public const string SectionName = "Kafka";
    public string BootstrapServers { get; set; } = "kafka:9092";
}
