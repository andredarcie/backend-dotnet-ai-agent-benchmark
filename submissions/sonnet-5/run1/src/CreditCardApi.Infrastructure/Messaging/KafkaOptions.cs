namespace CreditCardApi.Infrastructure.Messaging;

public class KafkaOptions
{
    public const string SectionName = "Kafka";

    public string BootstrapServers { get; set; } = "kafka:9092";

    public string TransactionsTopic { get; set; } = "transactions";

    public string DeadLetterTopic { get; set; } = "transactions.dlq";

    public string ConsumerGroupId { get; set; } = "credit-card-api";

    public int OutboxPollIntervalMs { get; set; } = 1000;

    public int OutboxBatchSize { get; set; } = 50;

    public int MaxDeliveryAttempts { get; set; } = 5;
}
