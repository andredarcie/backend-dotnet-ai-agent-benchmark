using Confluent.Kafka;
using Confluent.Kafka.Admin;
using CreditCardApi.Infrastructure.Configuration;
using Microsoft.Extensions.Options;
using Polly;

namespace CreditCardApi.Infrastructure.Messaging;

public sealed class KafkaTopicInitializer : BackgroundService
{
    private readonly IOptions<KafkaOptions> _options;
    private readonly ILogger<KafkaTopicInitializer> _logger;
    private readonly IAsyncPolicy _policy;

    public KafkaTopicInitializer(IOptions<KafkaOptions> options, ILogger<KafkaTopicInitializer> logger)
    {
        _options = options;
        _logger = logger;
        _policy = Policy
            .Handle<KafkaException>()
            .Or<CreateTopicsException>()
            .WaitAndRetryAsync(5, attempt => TimeSpan.FromSeconds(Math.Min(30, Math.Pow(2, attempt))));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await _policy.ExecuteAsync(async token =>
        {
            using var admin = new AdminClientBuilder(new AdminClientConfig { BootstrapServers = _options.Value.BootstrapServers }).Build();
            try
            {
                await admin.CreateTopicsAsync(
                    [
                        new TopicSpecification { Name = _options.Value.TransactionsTopic, NumPartitions = 3, ReplicationFactor = 1 },
                        new TopicSpecification { Name = _options.Value.DeadLetterTopic, NumPartitions = 1, ReplicationFactor = 1 }
                    ],
                    new CreateTopicsOptions { RequestTimeout = TimeSpan.FromSeconds(10) });

                _logger.LogInformation("Kafka topics are ready");
            }
            catch (CreateTopicsException exception) when (exception.Results.All(result => result.Error.Code == ErrorCode.TopicAlreadyExists))
            {
                _logger.LogInformation("Kafka topics already exist");
            }
        }, stoppingToken);
    }
}
