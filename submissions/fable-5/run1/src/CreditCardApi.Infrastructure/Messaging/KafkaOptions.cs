using System.ComponentModel.DataAnnotations;

namespace CreditCardApi.Infrastructure.Messaging;

/// <summary>
/// Kafka connectivity and topology settings, bound from the <c>Kafka</c> configuration section
/// (environment variables such as <c>Kafka__BootstrapServers</c>).
/// </summary>
public sealed class KafkaOptions
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "Kafka";

    /// <summary>Broker address list. Defaults to the in-compose address.</summary>
    [Required]
    public string BootstrapServers { get; set; } = "kafka:9092";

    /// <summary>Topic that receives one event per successfully created transaction.</summary>
    [Required]
    public string TransactionsTopic { get; set; } = "transactions";

    /// <summary>Dead-letter topic for events the consumer cannot process.</summary>
    [Required]
    public string DeadLetterTopic { get; set; } = "transactions.dlq";

    /// <summary>Consumer group id of this service's transactions-topic consumer.</summary>
    [Required]
    public string ConsumerGroupId { get; set; } = "credit-card-api";

    /// <summary>How often the outbox dispatcher polls for pending messages when idle.</summary>
    [Range(100, 60_000)]
    public int OutboxPollIntervalMs { get; set; } = 1_000;

    /// <summary>Maximum outbox messages dispatched per poll cycle.</summary>
    [Range(1, 1_000)]
    public int OutboxBatchSize { get; set; } = 50;
}
