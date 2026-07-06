using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;

namespace CreditCardApi.Infrastructure.Persistence;

/// <summary>Applies pending EF Core migrations at startup, retrying while Postgres is still coming up.</summary>
public class DatabaseInitializer(CreditCardDbContext dbContext, ILogger<DatabaseInitializer> logger)
{
    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        var pipeline = new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions
            {
                ShouldHandle = new PredicateBuilder().Handle<Exception>(),
                MaxRetryAttempts = 10,
                Delay = TimeSpan.FromSeconds(2),
                BackoffType = DelayBackoffType.Exponential,
                OnRetry = args =>
                {
                    logger.LogWarning(
                        args.Outcome.Exception,
                        "Database not ready yet (attempt {Attempt}); retrying...",
                        args.AttemptNumber + 1);
                    return ValueTask.CompletedTask;
                },
            })
            .Build();

        logger.LogInformation("Applying database migrations...");
        await pipeline.ExecuteAsync(async ct => await dbContext.Database.MigrateAsync(ct), cancellationToken);
        logger.LogInformation("Database migrations applied.");
    }
}
