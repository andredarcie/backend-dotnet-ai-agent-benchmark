using System.Text;
using Confluent.Kafka;
using CreditCardApi.Infrastructure.Observability;
using CreditCardApi.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CreditCardApi.Infrastructure.Messaging.Outbox;

/// <summary>
/// Background half of the transactional outbox: polls pending <see cref="OutboxMessage"/> rows in
/// insertion order and produces them to Kafka with a durable producer (acks=all, idempotence on).
/// A row is only marked processed after the broker acknowledges the write, so delivery is
/// at-least-once; consumers deduplicate by key.
/// </summary>
internal sealed class OutboxDispatcher : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IProducer<string, string> _producer;
    private readonly KafkaOptions _options;
    private readonly TimeProvider _clock;
    private readonly ILogger<OutboxDispatcher> _logger;

    public OutboxDispatcher(
        IServiceScopeFactory scopeFactory,
        IProducer<string, string> producer,
        IOptions<KafkaOptions> options,
        TimeProvider clock,
        ILogger<OutboxDispatcher> logger)
    {
        _scopeFactory = scopeFactory;
        _producer = producer;
        _options = options.Value;
        _clock = clock;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Outbox dispatcher started");
        using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(_options.OutboxPollIntervalMs));

        while (!stoppingToken.IsCancellationRequested)
        {
            var dispatched = 0;
            try
            {
                dispatched = await DispatchPendingAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Outbox dispatch cycle failed; will retry on the next poll");
            }

            // Drain a backlog without waiting; otherwise sleep until the next poll.
            if (dispatched < _options.OutboxBatchSize && !await WaitForNextTickAsync(timer, stoppingToken))
            {
                break;
            }
        }

        _logger.LogInformation("Outbox dispatcher stopped");
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await base.StopAsync(cancellationToken);

        // Graceful shutdown: give in-flight produce requests a moment to be acknowledged.
        _producer.Flush(TimeSpan.FromSeconds(5));
    }

    private static async Task<bool> WaitForNextTickAsync(PeriodicTimer timer, CancellationToken cancellationToken)
    {
        try
        {
            return await timer.WaitForNextTickAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            return false;
        }
    }

    private async Task<int> DispatchPendingAsync(CancellationToken cancellationToken)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<CreditCardDbContext>();

        var pending = await db.OutboxMessages
            .Where(m => m.ProcessedAt == null)
            .OrderBy(m => m.Id)
            .Take(_options.OutboxBatchSize)
            .ToListAsync(cancellationToken);

        if (pending.Count == 0)
        {
            return 0;
        }

        var dispatched = 0;
        foreach (var message in pending)
        {
            try
            {
                await _producer.ProduceAsync(message.Topic, ToKafkaMessage(message), cancellationToken);

                message.ProcessedAt = _clock.GetUtcNow().UtcDateTime;
                message.LastError = null;
                dispatched++;
            }
            catch (Exception ex) when (ex is ProduceException<string, string> or KafkaException)
            {
                message.Attempts++;
                message.LastError = ex.Message;
                _logger.LogWarning(
                    ex,
                    "Failed to publish outbox message {OutboxMessageId} (attempt {Attempts}); keeping it pending",
                    message.Id,
                    message.Attempts);

                // Stop the batch to preserve ordering; the poll loop retries from here.
                break;
            }
        }

        await db.SaveChangesAsync(cancellationToken);

        if (dispatched > 0)
        {
            _logger.LogInformation("Dispatched {Count} outbox message(s) to Kafka", dispatched);
        }

        return dispatched;
    }

    private static Message<string, string> ToKafkaMessage(OutboxMessage message)
    {
        var kafkaMessage = new Message<string, string>
        {
            Key = message.Key,
            Value = message.Payload,
        };

        if (message.CorrelationId is { Length: > 0 } correlationId)
        {
            kafkaMessage.Headers = [new Header(Correlation.HeaderName, Encoding.UTF8.GetBytes(correlationId))];
        }

        return kafkaMessage;
    }
}
