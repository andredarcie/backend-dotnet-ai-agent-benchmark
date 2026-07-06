using System;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using CreditCardApi.Application.Common.Interfaces;
using CreditCardApi.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Polly;

namespace CreditCardApi.Infrastructure.Messaging;

/// <summary>
/// Background service that processes OutboxMessages from the database and publishes them to Kafka.
/// </summary>
public class OutboxProcessorBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<OutboxProcessorBackgroundService> _logger;
    private readonly IAsyncPolicy _retryPolicy;

    public OutboxProcessorBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<OutboxProcessorBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;

        // Polly retry policy for Kafka publishing (exponential backoff)
        _retryPolicy = Policy
            .Handle<Exception>()
            .WaitAndRetryAsync(3, retryAttempt => 
                TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                (exception, timeSpan, retryCount, context) =>
                {
                    _logger.LogWarning(exception, 
                        "Transient error publishing outbox message. Retry count: {RetryCount}, Backoff: {TimeSpan}ms", 
                        retryCount, timeSpan.TotalMilliseconds);
                });
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Outbox Processor Background Service started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessOutboxMessagesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error processing outbox messages.");
            }

            // Wait 2 seconds before polling again
            await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
        }
    }

    private async Task ProcessOutboxMessagesAsync(CancellationToken stoppingToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var producer = scope.ServiceProvider.GetRequiredService<IKafkaProducer>();

        // Fetch up to 20 unprocessed outbox messages
        var messages = await context.OutboxMessages
            .Where(m => m.ProcessedOnUtc == null)
            .OrderBy(m => m.OccurredOnUtc)
            .Take(20)
            .ToListAsync(stoppingToken);

        if (messages.Count == 0)
        {
            return;
        }

        _logger.LogInformation("Processing {Count} outbox messages...", messages.Count);

        foreach (var message in messages)
        {
            try
            {
                // Execute publishing within Polly retry policy
                await _retryPolicy.ExecuteAsync(async () =>
                {
                    // For transactions, the key is the transaction ID.
                    // We parse the Content to extract the transaction ID (which is saved as "id": value).
                    // Or we can just use the OutboxMessage.Id. But the spec says:
                    // "Key: the transaction id as a string (so the same transaction always maps to the same key)."
                    // Since the payload contains "id": <number>, we can parse it to get the key.
                    var txnId = ExtractTransactionId(message.Content);
                    
                    await producer.PublishAsync("transactions", txnId, message.Content, stoppingToken);
                });

                message.ProcessedOnUtc = DateTime.UtcNow;
                message.Error = null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process outbox message {Id} after retries.", message.Id);
                message.Error = $"Failed after retries. Error: {ex.Message}";
                // Optionally log to dead-letter storage, but updating error allows us to avoid blocking the queue.
                // We'll mark it as processed but failed, or keep it unprocessed but logged.
                // To keep queue running, we can set ProcessedOnUtc = DateTime.UtcNow to skip it, and write error.
                // That way, a poison message doesn't block the rest of the API.
                message.ProcessedOnUtc = DateTime.UtcNow; // Skip poison message
            }
        }

        await context.SaveChangesAsync(stoppingToken);
    }

    private static string ExtractTransactionId(string jsonContent)
    {
        try
        {
            using var doc = JsonDocument.Parse(jsonContent);
            if (doc.RootElement.TryGetProperty("id", out var idProp))
            {
                return idProp.ToString();
            }
        }
        catch
        {
            // Fallback
        }
        return Guid.NewGuid().ToString(); // Fallback key if parsing fails
    }
}
