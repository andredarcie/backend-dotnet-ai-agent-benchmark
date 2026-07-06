using System.Text;
using System.Text.Json;
using Confluent.Kafka;
using CreditCardApi.Application.Transactions;
using CreditCardApi.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CreditCardApi.Infrastructure.Messaging.Consuming;

/// <summary>
/// An idempotent consumer of the <c>transactions</c> topic: dedupes by transaction id against a
/// processed-events ledger, commits offsets only after the effect is applied, and routes poison or
/// repeatedly-failing messages to the dead-letter topic instead of blocking the partition forever.
/// </summary>
public class TransactionEventsConsumer(
    IServiceScopeFactory scopeFactory,
    IProducer<string, string> deadLetterProducer,
    IOptions<KafkaOptions> options,
    ILogger<TransactionEventsConsumer> logger,
    TimeProvider timeProvider) : BackgroundService
{
    private static readonly JsonSerializerOptions PayloadSerializerOptions = new(JsonSerializerDefaults.Web);

    private readonly KafkaOptions _options = options.Value;

    protected override Task ExecuteAsync(CancellationToken stoppingToken) =>
        Task.Factory.StartNew(() => RunConsumeLoop(stoppingToken), stoppingToken, TaskCreationOptions.LongRunning, TaskScheduler.Default);

    private void RunConsumeLoop(CancellationToken stoppingToken)
    {
        var config = new ConsumerConfig
        {
            BootstrapServers = _options.BootstrapServers,
            GroupId = _options.ConsumerGroupId,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false,
        };

        using var consumer = new ConsumerBuilder<string, string>(config).Build();
        consumer.Subscribe(_options.TransactionsTopic);

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                ConsumeResult<string, string>? result;
                try
                {
                    result = consumer.Consume(stoppingToken);
                }
                catch (ConsumeException ex)
                {
                    logger.LogError(ex, "Failed to read from {Topic}; skipping to the next message.", _options.TransactionsTopic);
                    continue;
                }

                if (result?.Message is null)
                {
                    continue;
                }

                ProcessMessage(result, stoppingToken);
                consumer.Commit(result);
            }
        }
        catch (OperationCanceledException)
        {
            // Graceful shutdown.
        }
        finally
        {
            consumer.Close();
        }
    }

    private void ProcessMessage(ConsumeResult<string, string> result, CancellationToken cancellationToken)
    {
        TransactionResponse? payload;
        try
        {
            payload = JsonSerializer.Deserialize<TransactionResponse>(result.Message.Value, PayloadSerializerOptions);
        }
        catch (JsonException ex)
        {
            logger.LogError(ex, "Poison message on {Topic} at offset {Offset}; sending to the dead-letter topic.", result.Topic, result.Offset);
            SendToDeadLetter(result, "deserialize-failure");
            return;
        }

        if (payload is null || payload.Id <= 0)
        {
            logger.LogError("Empty or invalid payload on {Topic} at offset {Offset}; sending to the dead-letter topic.", result.Topic, result.Offset);
            SendToDeadLetter(result, "empty-payload");
            return;
        }

        try
        {
            ApplyIdempotentlyAsync(payload.Id, cancellationToken).GetAwaiter().GetResult();
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Failed to apply transaction event {TransactionId}; sending to the dead-letter topic.", payload.Id);
            SendToDeadLetter(result, "processing-failure");
        }
    }

    private async Task ApplyIdempotentlyAsync(int transactionId, CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CreditCardDbContext>();

        var alreadyProcessed = await dbContext.ConsumedTransactionEvents
            .AsNoTracking()
            .AnyAsync(e => e.TransactionId == transactionId, cancellationToken);

        if (alreadyProcessed)
        {
            logger.LogInformation("Transaction event {TransactionId} already processed; skipping (idempotent).", transactionId);
            return;
        }

        dbContext.ConsumedTransactionEvents.Add(new ConsumedTransactionEvent(transactionId, timeProvider.GetUtcNow().UtcDateTime));

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            // Another instance recorded the same event concurrently (unique PK race) — also idempotent.
            logger.LogInformation("Transaction event {TransactionId} recorded concurrently by another instance.", transactionId);
        }
    }

    private void SendToDeadLetter(ConsumeResult<string, string> result, string reason)
    {
        var headers = new Headers { { "x-dead-letter-reason", Encoding.UTF8.GetBytes(reason) } };
        deadLetterProducer.Produce(
            _options.DeadLetterTopic,
            new Message<string, string> { Key = result.Message.Key, Value = result.Message.Value, Headers = headers });
        deadLetterProducer.Flush(TimeSpan.FromSeconds(5));
    }
}
