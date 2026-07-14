using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;

namespace CreditCardApi.Infrastructure.Persistence;

/// <summary>Applies pending EF Core migrations on startup, retrying while Postgres is still coming up.</summary>
public static class DatabaseMigrator
{
    public static async Task MigrateAsync(IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<AppDbContext>>();

        var pipeline = new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions
            {
                MaxRetryAttempts = 10,
                BackoffType = DelayBackoffType.Exponential,
                Delay = TimeSpan.FromSeconds(2),
                MaxDelay = TimeSpan.FromSeconds(15),
                OnRetry = args =>
                {
                    logger.LogWarning("Database not ready yet, retrying migration (attempt {Attempt}): {Reason}",
                        args.AttemptNumber + 1, args.Outcome.Exception?.Message);
                    return ValueTask.CompletedTask;
                },
            })
            .Build();

        await pipeline.ExecuteAsync(async ct => await dbContext.Database.MigrateAsync(ct), cancellationToken);
    }
}
