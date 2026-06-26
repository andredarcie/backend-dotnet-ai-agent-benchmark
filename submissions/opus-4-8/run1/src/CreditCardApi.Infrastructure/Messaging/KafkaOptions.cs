namespace CreditCardApi.Infrastructure.Messaging;

/// <summary>Kafka connection and topic settings, bound from the <c>Kafka</c> configuration section.</summary>
public sealed class KafkaOptions
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "Kafka";

    /// <summary>Broker address. Defaults to the in-cluster listener.</summary>
    public string BootstrapServers { get; set; } = "kafka:9092";

    /// <summary>Topic that created transactions are published to.</summary>
    public string TransactionsTopic { get; set; } = "transactions";

    /// <summary>Dead-letter topic for messages that cannot be processed.</summary>
    public string DeadLetterTopic { get; set; } = "transactions.DLQ";

    /// <summary>Consumer group id for the in-process projection consumer.</summary>
    public string ConsumerGroupId { get; set; } = "credit-card-api";

    /// <summary>Whether to run the in-process idempotent consumer.</summary>
    public bool EnableConsumer { get; set; } = true;

    /// <summary>Partition count used when auto-creating the main topic.</summary>
    public int TopicPartitions { get; set; } = 3;

    /// <summary>Replication factor used when auto-creating topics (1 for the single-broker dev cluster).</summary>
    public short TopicReplicationFactor { get; set; } = 1;
}
