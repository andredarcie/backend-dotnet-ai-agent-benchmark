namespace CreditCardApi.Messaging;

public class KafkaSettings
{
    public const string SectionName = "Kafka";

    /// <summary>Broker bootstrap address. Defaults to the internal Docker listener.</summary>
    public string BootstrapServers { get; set; } = "kafka:9092";

    public string TransactionsTopic { get; set; } = "transactions";
}
