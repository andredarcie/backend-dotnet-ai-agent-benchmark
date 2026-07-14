using Confluent.Kafka;
using Confluent.Kafka.Admin;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Retry;

namespace CreditCardApi.Infrastructure.Messaging;

/// <summary>Ensures the transactions topic exists on startup, retrying while the broker is still coming up.</summary>
public sealed class KafkaTopicInitializer(IOptions<KafkaOptions> options, ILogger<KafkaTopicInitializer> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var kafkaOptions = options.Value;
        using var adminClient = new AdminClientBuilder(new AdminClientConfig { BootstrapServers = kafkaOptions.BootstrapServers }).Build();

        var pipeline = new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions
            {
                MaxRetryAttempts = 10,
                BackoffType = DelayBackoffType.Exponential,
                Delay = TimeSpan.FromSeconds(1),
                MaxDelay = TimeSpan.FromSeconds(10),
                OnRetry = args =>
                {
                    logger.LogWarning(
                        "Kafka not ready yet, retrying topic creation for '{Topic}' (attempt {Attempt}): {Reason}",
                        kafkaOptions.TransactionsTopic, args.AttemptNumber + 1, args.Outcome.Exception?.Message);
                    return ValueTask.CompletedTask;
                },
            })
            .Build();

        try
        {
            await pipeline.ExecuteAsync(
                async _ => await CreateTopicIfMissingAsync(adminClient, kafkaOptions.TransactionsTopic),
                cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Could not ensure Kafka topic '{Topic}' exists after retries; the producer will still attempt to publish to it.",
                kafkaOptions.TransactionsTopic);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    internal async Task CreateTopicIfMissingAsync(IAdminClient adminClient, string topic)
    {
        try
        {
            await adminClient.CreateTopicsAsync(
            [
                new TopicSpecification { Name = topic, NumPartitions = 1, ReplicationFactor = 1 },
            ]);
            logger.LogInformation("Kafka topic '{Topic}' created.", topic);
        }
        catch (CreateTopicsException ex) when (ex.Results.All(r => r.Error.Code == ErrorCode.TopicAlreadyExists))
        {
            logger.LogInformation("Kafka topic '{Topic}' already exists.", topic);
        }
    }
}
