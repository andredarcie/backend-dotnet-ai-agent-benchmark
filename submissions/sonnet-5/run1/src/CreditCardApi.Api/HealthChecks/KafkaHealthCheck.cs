using Confluent.Kafka;
using CreditCardApi.Infrastructure.Messaging;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace CreditCardApi.Api.HealthChecks;

public class KafkaHealthCheck(IOptions<KafkaOptions> options) : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            using var adminClient = new AdminClientBuilder(
                new AdminClientConfig { BootstrapServers = options.Value.BootstrapServers }).Build();

            var metadata = adminClient.GetMetadata(TimeSpan.FromSeconds(5));
            return Task.FromResult(metadata.Brokers.Count > 0
                ? HealthCheckResult.Healthy($"Kafka reachable ({metadata.Brokers.Count} broker(s)).")
                : HealthCheckResult.Unhealthy("Kafka reported no brokers."));
        }
        catch (Exception ex)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy("Kafka health check threw.", ex));
        }
    }
}
