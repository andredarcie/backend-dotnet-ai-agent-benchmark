using Confluent.Kafka;
using CreditCardApi.Infrastructure.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace CreditCardApi.Infrastructure.Health;

public sealed class KafkaHealthCheck : IHealthCheck
{
    private readonly IOptions<KafkaOptions> _options;

    public KafkaHealthCheck(IOptions<KafkaOptions> options)
    {
        _options = options;
    }

    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            using var admin = new AdminClientBuilder(new AdminClientConfig { BootstrapServers = _options.Value.BootstrapServers }).Build();
            var metadata = admin.GetMetadata(_options.Value.TransactionsTopic, TimeSpan.FromSeconds(5));
            return Task.FromResult(metadata.Topics.Count > 0
                ? HealthCheckResult.Healthy()
                : HealthCheckResult.Unhealthy("Kafka topic metadata was empty."));
        }
        catch (KafkaException exception)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy("Kafka is unavailable.", exception));
        }
    }
}
