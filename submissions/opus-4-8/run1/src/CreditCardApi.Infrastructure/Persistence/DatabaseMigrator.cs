using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;

namespace CreditCardApi.Infrastructure.Persistence;

/// <summary>Applies EF Core migrations on startup, waiting for the database to become available.</summary>
public static class DatabaseMigrator
{
    /// <summary>Runs all pending migrations, retrying while the database is not yet reachable.</summary>
    public static async Task MigrateDatabaseAsync(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger(nameof(DatabaseMigrator));

        var pipeline = new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions
            {
                MaxRetryAttempts = 12,
                Delay = TimeSpan.FromSeconds(3),
                BackoffType = DelayBackoffType.Constant,
                OnRetry = args =>
                {
                    logger.LogWarning("Database not ready (attempt {Attempt}); retrying migration.",
                        args.AttemptNumber + 1);
                    return ValueTask.CompletedTask;
                },
            })
            .Build();

        await pipeline.ExecuteAsync(async token => await db.Database.MigrateAsync(token), cancellationToken);
        logger.LogInformation("Database migrations applied.");
    }
}
