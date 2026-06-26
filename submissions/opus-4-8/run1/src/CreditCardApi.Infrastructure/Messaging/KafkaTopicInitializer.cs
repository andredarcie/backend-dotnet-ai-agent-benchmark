using Confluent.Kafka;
using Confluent.Kafka.Admin;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Retry;

namespace CreditCardApi.Infrastructure.Messaging;

/// <summary>
/// Creates the required Kafka topics on startup (idempotently), retrying until the broker is reachable.
/// </summary>
public sealed class KafkaTopicInitializer : IHostedService
{
    private readonly KafkaOptions _options;
    private readonly ILogger<KafkaTopicInitializer> _logger;

    /// <summary>Creates the initializer.</summary>
    public KafkaTopicInitializer(IOptions<KafkaOptions> options, ILogger<KafkaTopicInitializer> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var pipeline = new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions
            {
                MaxRetryAttempts = 15,
                Delay = TimeSpan.FromSeconds(3),
                BackoffType = DelayBackoffType.Constant,
                OnRetry = args =>
                {
                    _logger.LogWarning("Kafka not ready (attempt {Attempt}); retrying topic creation.",
                        args.AttemptNumber + 1);
                    return ValueTask.CompletedTask;
                },
            })
            .Build();

        await pipeline.ExecuteAsync(async token => await CreateTopicsAsync(token), cancellationToken);
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private async Task CreateTopicsAsync(CancellationToken cancellationToken)
    {
        var config = new AdminClientConfig { BootstrapServers = _options.BootstrapServers };
        using var admin = new AdminClientBuilder(config).Build();

        var specs = new[]
        {
            new TopicSpecification
            {
                Name = _options.TransactionsTopic,
                NumPartitions = _options.TopicPartitions,
                ReplicationFactor = _options.TopicReplicationFactor,
            },
            new TopicSpecification
            {
                Name = _options.DeadLetterTopic,
                NumPartitions = 1,
                ReplicationFactor = _options.TopicReplicationFactor,
            },
        };

        try
        {
            await admin.CreateTopicsAsync(specs);
            _logger.LogInformation("Created Kafka topics: {Topics}",
                string.Join(", ", specs.Select(s => s.Name)));
        }
        catch (CreateTopicsException ex) when (ex.Results.All(r => r.Error.Code == ErrorCode.TopicAlreadyExists))
        {
            _logger.LogInformation("Kafka topics already exist; nothing to create.");
        }
    }
}
