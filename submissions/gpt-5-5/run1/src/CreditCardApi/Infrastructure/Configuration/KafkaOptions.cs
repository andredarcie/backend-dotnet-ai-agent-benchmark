using System.ComponentModel.DataAnnotations;

namespace CreditCardApi.Infrastructure.Configuration;

public sealed class KafkaOptions
{
    [Required]
    public string BootstrapServers { get; init; } = "kafka:9092";

    [Required]
    public string TransactionsTopic { get; init; } = "transactions";

    [Required]
    public string DeadLetterTopic { get; init; } = "transactions.dlq";

    [Required]
    public string ConsumerGroupId { get; init; } = "credit-card-api";

    [Range(1, 100)]
    public int MaxOutboxAttempts { get; init; } = 5;

    [Range(1, 60)]
    public int OutboxPollSeconds { get; init; } = 2;
}
