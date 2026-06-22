using Confluent.Kafka;
using Confluent.Kafka.Admin;
using Microsoft.Extensions.Options;

namespace CreditCardApi.Messaging;

/// <summary>
/// Ensures the <c>transactions</c> topic exists on startup. Best-effort with retries:
/// if the broker is not yet reachable it falls back to broker-side auto-creation and
/// never blocks the API from serving requests.
/// </summary>
public class KafkaTopicInitializer : IHostedService
{
    private readonly KafkaSettings _settings;
    private readonly ILogger<KafkaTopicInitializer> _logger;

    public KafkaTopicInitializer(IOptions<KafkaSettings> options, ILogger<KafkaTopicInitializer> logger)
    {
        _settings = options.Value;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        const int maxAttempts = 10;

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                using var admin = new AdminClientBuilder(new AdminClientConfig
                {
                    BootstrapServers = _settings.BootstrapServers
                }).Build();

                await admin.CreateTopicsAsync(new[]
                {
                    new TopicSpecification
                    {
                        Name = _settings.TransactionsTopic,
                        NumPartitions = 1,
                        ReplicationFactor = 1
                    }
                });

                _logger.LogInformation("Created Kafka topic '{Topic}'.", _settings.TransactionsTopic);
                return;
            }
            catch (CreateTopicsException ex)
                when (ex.Results.All(r => r.Error.Code == ErrorCode.TopicAlreadyExists))
            {
                _logger.LogInformation("Kafka topic '{Topic}' already exists.", _settings.TransactionsTopic);
                return;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    "Could not ensure Kafka topic (attempt {Attempt}/{Max}): {Message}",
                    attempt, maxAttempts, ex.Message);

                await Task.Delay(TimeSpan.FromSeconds(3), cancellationToken);
            }
        }

        _logger.LogWarning(
            "Gave up pre-creating Kafka topic '{Topic}'; relying on broker auto-creation.",
            _settings.TransactionsTopic);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
