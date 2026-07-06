using CreditCardApi.Infrastructure.Persistence;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace CreditCardApi.Api.HealthChecks;

public class PostgresHealthCheck(CreditCardDbContext dbContext) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            return await dbContext.Database.CanConnectAsync(cancellationToken)
                ? HealthCheckResult.Healthy("Postgres is reachable.")
                : HealthCheckResult.Unhealthy("Postgres did not respond.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Postgres health check threw.", ex);
        }
    }
}
