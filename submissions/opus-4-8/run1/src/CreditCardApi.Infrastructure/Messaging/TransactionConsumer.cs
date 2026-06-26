using Confluent.Kafka;
using CreditCardApi.Application.Abstractions;
using CreditCardApi.Infrastructure.Persistence;
using CreditCardApi.Infrastructure.Persistence.Inbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CreditCardApi.Infrastructure.Messaging;

/// <summary>
/// Consumes the transactions topic as a downstream projection. It is idempotent (it skips keys it has
/// already recorded), commits offsets only after a message is handled, and dead-letters poison messages.
/// </summary>
public sealed class TransactionConsumer : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly KafkaProducer _producer;
    private readonly KafkaOptions _options;
    private readonly IClock _clock;
    private readonly ILogger<TransactionConsumer> _logger;

    /// <summary>Creates the consumer.</summary>
    public TransactionConsumer(
        IServiceScopeFactory scopeFactory,
        KafkaProducer producer,
        IOptions<KafkaOptions> options,
        IClock clock,
        ILogger<TransactionConsumer> logger)
    {
        _scopeFactory = scopeFactory;
        _producer = producer;
        _options = options.Value;
        _clock = clock;
        _logger = logger;
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.EnableConsumer)
        {
            return;
        }

        var config = new ConsumerConfig
        {
            BootstrapServers = _options.BootstrapServers,
            GroupId = _options.ConsumerGroupId,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false, // commit explicitly, only after processing
            EnableAutoOffsetStore = false,
        };

        using var consumer = new ConsumerBuilder<string, string>(config)
            .SetErrorHandler((_, error) =>
                _logger.LogError("Kafka consumer error: {Code} {Reason}", error.Code, error.Reason))
            .Build();

        consumer.Subscribe(_options.TransactionsTopic);
        _logger.LogInformation("Consuming topic {Topic} as group {Group}.",
            _options.TransactionsTopic, _options.ConsumerGroupId);

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                ConsumeResult<string, string>? result;
                try
                {
                    result = consumer.Consume(TimeSpan.FromSeconds(1));
                }
                catch (ConsumeException ex)
                {
                    _logger.LogError(ex, "Error consuming from {Topic}.", _options.TransactionsTopic);
                    continue;
                }

                if (result is null)
                {
                    continue;
                }

                await HandleAsync(consumer, result, stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            // Normal shutdown.
        }
        finally
        {
            consumer.Close(); // leave the group cleanly and commit final offsets
        }
    }

    private async Task HandleAsync(
        IConsumer<string, string> consumer,
        ConsumeResult<string, string> result,
        CancellationToken cancellationToken)
    {
        var key = result.Message.Key ?? string.Empty;
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var alreadyProcessed = await db.ProcessedMessages
                .AnyAsync(m => m.Topic == _options.TransactionsTopic && m.MessageKey == key, cancellationToken);

            if (alreadyProcessed)
            {
                _logger.LogDebug("Skipping already-processed message with key {Key}.", key);
            }
            else
            {
                // A real projection/audit would run here. We record the key to stay idempotent.
                db.ProcessedMessages.Add(new ProcessedMessage
                {
                    Topic = _options.TransactionsTopic,
                    MessageKey = key,
                    ProcessedAt = _clock.UtcNow,
                });
                await db.SaveChangesAsync(cancellationToken);
                _logger.LogInformation("Processed transaction event with key {Key}.", key);
            }

            consumer.Commit(result);
        }
        catch (DbUpdateException)
        {
            // A concurrent duplicate hit the idempotency key; it is safe to commit and move on.
            consumer.Commit(result);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Failed to process message with key {Key}; routing to dead-letter topic.", key);
            await RouteToDeadLetterAsync(result, cancellationToken);
            consumer.Commit(result); // do not let a poison message block the partition
        }
    }

    private async Task RouteToDeadLetterAsync(ConsumeResult<string, string> result, CancellationToken cancellationToken)
    {
        try
        {
            await _producer.ProduceAsync(
                _options.DeadLetterTopic,
                result.Message.Key ?? string.Empty,
                result.Message.Value,
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish message to the dead-letter topic.");
        }
    }
}
