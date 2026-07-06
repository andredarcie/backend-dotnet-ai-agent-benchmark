using CreditCardApi.Data;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Polly;

namespace CreditCardApi.Infrastructure;

public sealed class DatabaseMigrationService : IHostedService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<DatabaseMigrationService> _logger;
    private readonly IAsyncPolicy _policy;

    public DatabaseMigrationService(IServiceScopeFactory scopeFactory, ILogger<DatabaseMigrationService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _policy = Policy
            .Handle<NpgsqlException>()
            .Or<TimeoutException>()
            .WaitAndRetryAsync(10, attempt => TimeSpan.FromSeconds(Math.Min(30, Math.Pow(2, attempt))));
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await _policy.ExecuteAsync(async token =>
        {
            using var scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            await dbContext.Database.MigrateAsync(token);
            _logger.LogInformation("Database migrations applied");
        }, cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
