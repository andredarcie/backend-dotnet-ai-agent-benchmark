using Confluent.Kafka;
using CreditCardApi.Infrastructure.Messaging;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace CreditCardApi.Infrastructure.HealthChecks;

/// <summary>
/// A ~15-line admin-client metadata probe. Simpler and fully under our control compared to
/// wiring a community Kafka health-check package around the same IOptions-bound broker address.
/// </summary>
public sealed class KafkaHealthCheck(IOptions<KafkaOptions> options) : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            using var adminClient = new AdminClientBuilder(new AdminClientConfig { BootstrapServers = options.Value.BootstrapServers }).Build();
            var metadata = adminClient.GetMetadata(TimeSpan.FromSeconds(5));

            return Task.FromResult(metadata.Brokers.Count > 0
                ? HealthCheckResult.Healthy($"{metadata.Brokers.Count} broker(s) reachable.")
                : HealthCheckResult.Unhealthy("No Kafka brokers returned by the metadata request."));
        }
        catch (Exception ex)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy("Kafka broker unreachable.", ex));
        }
    }
}
