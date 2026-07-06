using System.Globalization;
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

namespace CreditCardApi.Infrastructure.Messaging.Outbox;

/// <summary>
/// The durable producer half of the transactional outbox: polls for unprocessed rows and publishes
/// them with acks=all + idempotence, only marking a row processed once the broker has acknowledged it.
/// Rows that keep failing are re-routed to the dead-letter topic instead of retried forever.
/// </summary>
public class OutboxDispatcher(
    IServiceScopeFactory scopeFactory,
    IProducer<string, string> producer,
    IOptions<KafkaOptions> options,
    ILogger<OutboxDispatcher> logger,
    TimeProvider timeProvider) : BackgroundService
{
    private static readonly JsonSerializerOptions PayloadSerializerOptions = new(JsonSerializerDefaults.Web);

    private readonly KafkaOptions _options = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(_options.OutboxPollIntervalMs));
        do
        {
            try
            {
                await DispatchPendingAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Outbox dispatch cycle failed; will retry on the next tick.");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task DispatchPendingAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CreditCardDbContext>();

        var pending = await dbContext.OutboxMessages
            .Include(m => m.Transaction)
            .Where(m => m.ProcessedAt == null)
            .OrderBy(m => m.Id)
            .Take(_options.OutboxBatchSize)
            .ToListAsync(cancellationToken);

        if (pending.Count == 0)
        {
            return;
        }

        foreach (var message in pending)
        {
            await DispatchOneAsync(message, cancellationToken);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task DispatchOneAsync(OutboxMessage message, CancellationToken cancellationToken)
    {
        if (message.Transaction is null)
        {
            // The transaction was deleted before its event could be published; nothing meaningful to send.
            message.MarkProcessed(timeProvider.GetUtcNow().UtcDateTime);
            return;
        }

        var isDeadLetter = message.AttemptCount >= _options.MaxDeliveryAttempts;
        var targetTopic = isDeadLetter ? _options.DeadLetterTopic : message.Topic;
        var payload = JsonSerializer.Serialize(TransactionMapping.ToResponse(message.Transaction), PayloadSerializerOptions);

        var kafkaMessage = new Message<string, string>
        {
            Key = message.TransactionId.ToString(CultureInfo.InvariantCulture),
            Value = payload,
            Headers = BuildHeaders(message, isDeadLetter),
        };

        try
        {
            await producer.ProduceAsync(targetTopic, kafkaMessage, cancellationToken);
            message.MarkProcessed(timeProvider.GetUtcNow().UtcDateTime);

            if (isDeadLetter)
            {
                logger.LogWarning(
                    "Outbox message {Id} for transaction {TransactionId} dead-lettered after {Attempts} failed attempts.",
                    message.Id, message.TransactionId, message.AttemptCount);
            }
        }
        catch (ProduceException<string, string> ex)
        {
            message.RecordFailedAttempt(ex.Message);
            logger.LogError(
                ex, "Failed to publish outbox message {Id} for transaction {TransactionId} (attempt {Attempt}).",
                message.Id, message.TransactionId, message.AttemptCount);
        }
    }

    private static Headers BuildHeaders(OutboxMessage message, bool isDeadLetter)
    {
        var headers = new Headers();
        if (message.CorrelationId is not null)
        {
            headers.Add("x-correlation-id", Encoding.UTF8.GetBytes(message.CorrelationId));
        }

        if (isDeadLetter)
        {
            headers.Add("x-dead-letter-reason", Encoding.UTF8.GetBytes("max-delivery-attempts-exceeded"));
        }

        return headers;
    }
}
