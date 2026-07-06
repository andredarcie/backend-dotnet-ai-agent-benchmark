using Confluent.Kafka;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace CreditCardApi.Api.HealthChecks;

/// <summary>Readiness check that verifies the Kafka broker answers a metadata request.</summary>
internal sealed class KafkaHealthCheck : IHealthCheck
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(2);

    private readonly IAdminClient _adminClient;

    public KafkaHealthCheck(IAdminClient adminClient)
    {
        _adminClient = adminClient;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // GetMetadata blocks, so it runs off the request thread.
            var metadata = await Task.Run(() => _adminClient.GetMetadata(Timeout), cancellationToken);
            return HealthCheckResult.Healthy($"Kafka reachable ({metadata.Brokers.Count} broker(s))");
        }
        catch (KafkaException ex)
        {
            return HealthCheckResult.Unhealthy("Kafka broker unreachable", ex);
        }
    }
}
