using Confluent.Kafka;
using Confluent.Kafka.Admin;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Retry;

namespace CreditCardApi.Infrastructure.Messaging;

/// <summary>Idempotently ensures the topics this service needs exist before the app starts serving traffic.</summary>
public class KafkaTopicInitializer(IOptions<KafkaOptions> options, ILogger<KafkaTopicInitializer> logger)
{
    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        var kafkaOptions = options.Value;
        using var adminClient = new AdminClientBuilder(new AdminClientConfig
        {
            BootstrapServers = kafkaOptions.BootstrapServers,
        }).Build();

        var pipeline = new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions
            {
                ShouldHandle = new PredicateBuilder().Handle<Exception>(),
                MaxRetryAttempts = 10,
                Delay = TimeSpan.FromSeconds(2),
                BackoffType = DelayBackoffType.Exponential,
                OnRetry = args =>
                {
                    logger.LogWarning(
                        args.Outcome.Exception, "Kafka not ready yet (attempt {Attempt}); retrying...", args.AttemptNumber + 1);
                    return ValueTask.CompletedTask;
                },
            })
            .Build();

        var specs = new[]
        {
            new TopicSpecification { Name = kafkaOptions.TransactionsTopic, NumPartitions = 3, ReplicationFactor = 1 },
            new TopicSpecification { Name = kafkaOptions.DeadLetterTopic, NumPartitions = 1, ReplicationFactor = 1 },
        };

        await pipeline.ExecuteAsync(
            async _ =>
            {
                try
                {
                    await adminClient.CreateTopicsAsync(specs);
                    logger.LogInformation("Kafka topics ensured: {Topics}", string.Join(", ", specs.Select(s => s.Name)));
                }
                catch (CreateTopicsException ex) when (ex.Results.All(r => r.Error.Code == ErrorCode.TopicAlreadyExists))
                {
                    logger.LogInformation("Kafka topics already exist.");
                }
            },
            cancellationToken);
    }
}
