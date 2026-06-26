using CreditCardApi.Application.Abstractions;
using CreditCardApi.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CreditCardApi.Infrastructure.Messaging;

/// <summary>
/// Background processor that drains the outbox and publishes each event to Kafka. On repeated failure
/// an event is routed to the dead-letter topic so a single poison event cannot block the queue.
/// </summary>
public sealed class OutboxDispatcher : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly KafkaProducer _producer;
    private readonly KafkaOptions _kafka;
    private readonly OutboxOptions _options;
    private readonly IClock _clock;
    private readonly ILogger<OutboxDispatcher> _logger;

    /// <summary>Creates the dispatcher.</summary>
    public OutboxDispatcher(
        IServiceScopeFactory scopeFactory,
        KafkaProducer producer,
        IOptions<KafkaOptions> kafka,
        IOptions<OutboxOptions> options,
        IClock clock,
        ILogger<OutboxDispatcher> logger)
    {
        _scopeFactory = scopeFactory;
        _producer = producer;
        _kafka = kafka.Value;
        _options = options.Value;
        _clock = clock;
        _logger = logger;
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var interval = TimeSpan.FromSeconds(Math.Max(1, _options.PollIntervalSeconds));
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await DispatchPendingAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Outbox dispatch cycle failed; will retry next interval.");
            }

            try
            {
                await Task.Delay(interval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    private async Task DispatchPendingAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var pending = await db.OutboxMessages
            .Where(m => m.ProcessedAt == null)
            .OrderBy(m => m.CreatedAt)
            .Take(_options.BatchSize)
            .ToListAsync(cancellationToken);

        if (pending.Count == 0)
        {
            return;
        }

        foreach (var message in pending)
        {
            try
            {
                await _producer.ProduceAsync(message.Topic, message.Key, message.Payload, cancellationToken);
                message.ProcessedAt = _clock.UtcNow;
            }
            catch (Exception ex)
            {
                message.Attempts++;
                message.LastError = ex.Message;

                if (message.Attempts >= _options.MaxAttempts)
                {
                    await RouteToDeadLetterAsync(message.Key, message.Payload, cancellationToken);
                    message.ProcessedAt = _clock.UtcNow;
                    _logger.LogError(ex,
                        "Outbox message {Id} exceeded {Max} attempts; routed to dead-letter topic.",
                        message.Id, _options.MaxAttempts);
                }
                else
                {
                    _logger.LogWarning(ex,
                        "Failed to publish outbox message {Id} (attempt {Attempts}); will retry.",
                        message.Id, message.Attempts);
                }
            }
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task RouteToDeadLetterAsync(string key, string payload, CancellationToken cancellationToken)
    {
        try
        {
            await _producer.ProduceAsync(_kafka.DeadLetterTopic, key, payload, cancellationToken);
        }
        catch (Exception dlqEx)
        {
            _logger.LogError(dlqEx, "Failed to route outbox message {Key} to the dead-letter topic.", key);
        }
    }
}
