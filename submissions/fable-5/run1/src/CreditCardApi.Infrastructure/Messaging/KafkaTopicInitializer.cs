using Confluent.Kafka;
using Confluent.Kafka.Admin;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Retry;

namespace CreditCardApi.Infrastructure.Messaging;

/// <summary>
/// Creates the application's Kafka topics at startup (idempotently), retrying until the broker is
/// reachable, so the first transaction event never races topic creation.
/// </summary>
public static class KafkaTopicInitializer
{
    /// <summary>Ensures the transactions and dead-letter topics exist.</summary>
    public static async Task EnsureTopicsAsync(IServiceProvider services, CancellationToken cancellationToken)
    {
        var options = services.GetRequiredService<IOptions<KafkaOptions>>().Value;
        var adminClient = services.GetRequiredService<IAdminClient>();
        var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger(typeof(KafkaTopicInitializer));

        var topics = new[]
        {
            new TopicSpecification { Name = options.TransactionsTopic, NumPartitions = 3, ReplicationFactor = 1 },
            new TopicSpecification { Name = options.DeadLetterTopic, NumPartitions = 1, ReplicationFactor = 1 },
        };

        var pipeline = new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions
            {
                ShouldHandle = new PredicateBuilder().Handle<Exception>(ex => ex is not OperationCanceledException),
                MaxRetryAttempts = 10,
                Delay = TimeSpan.FromSeconds(1),
                BackoffType = DelayBackoffType.Exponential,
                UseJitter = true,
                MaxDelay = TimeSpan.FromSeconds(15),
                OnRetry = args =>
                {
                    logger.LogWarning(
                        args.Outcome.Exception,
                        "Kafka broker not ready (attempt {Attempt}); retrying in {Delay}",
                        args.AttemptNumber + 1,
                        args.RetryDelay);
                    return ValueTask.CompletedTask;
                },
            })
            .Build();

        await pipeline.ExecuteAsync(
            async _ =>
            {
                try
                {
                    await adminClient.CreateTopicsAsync(topics);
                    logger.LogInformation(
                        "Created Kafka topics {Topics}",
                        string.Join(", ", topics.Select(t => t.Name)));
                }
                catch (CreateTopicsException ex) when (ex.Results.TrueForAll(
                    r => r.Error.Code is ErrorCode.TopicAlreadyExists or ErrorCode.NoError))
                {
                    logger.LogInformation("Kafka topics already exist");
                }
            },
            cancellationToken);
    }
}
