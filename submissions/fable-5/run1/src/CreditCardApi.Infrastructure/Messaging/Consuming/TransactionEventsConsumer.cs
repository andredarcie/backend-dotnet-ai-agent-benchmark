using System.Text;
using System.Text.Json;
using Confluent.Kafka;
using CreditCardApi.Application.Transactions;
using CreditCardApi.Infrastructure.Observability;
using CreditCardApi.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;

namespace CreditCardApi.Infrastructure.Messaging.Consuming;

/// <summary>
/// Consumes the <c>transactions</c> topic and records each event exactly once in the
/// <see cref="ConsumedTransactionEvent"/> ledger.
/// </summary>
/// <remarks>
/// Demonstrates the standard consumer guarantees:
/// <list type="bullet">
/// <item><description><b>Idempotence</b> — events are deduplicated by transaction id (primary key), so redeliveries are no-ops.</description></item>
/// <item><description><b>Commit after processing</b> — offsets are committed manually, only once the event is durably handled.</description></item>
/// <item><description><b>Dead-letter path</b> — unparseable messages go straight to the DLQ; transient failures are retried in place a few times and then dead-lettered.</description></item>
/// </list>
/// </remarks>
internal sealed class TransactionEventsConsumer : BackgroundService
{
    private const int MaxDeliveryAttempts = 5;
    private static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(2);
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IProducer<string, string> _producer;
    private readonly KafkaOptions _options;
    private readonly TimeProvider _clock;
    private readonly ILogger<TransactionEventsConsumer> _logger;

    public TransactionEventsConsumer(
        IServiceScopeFactory scopeFactory,
        IProducer<string, string> producer,
        IOptions<KafkaOptions> options,
        TimeProvider clock,
        ILogger<TransactionEventsConsumer> logger)
    {
        _scopeFactory = scopeFactory;
        _producer = producer;
        _options = options.Value;
        _clock = clock;
        _logger = logger;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken) =>
        // Consume() blocks, so the loop gets a dedicated thread instead of starving the pool.
        Task.Factory.StartNew(
            () => ConsumeLoopAsync(stoppingToken),
            stoppingToken,
            TaskCreationOptions.LongRunning,
            TaskScheduler.Default).Unwrap();

    private async Task ConsumeLoopAsync(CancellationToken stoppingToken)
    {
        var config = new ConsumerConfig
        {
            BootstrapServers = _options.BootstrapServers,
            GroupId = _options.ConsumerGroupId,
            // Offsets are committed manually, strictly after successful processing.
            EnableAutoCommit = false,
            AutoOffsetReset = AutoOffsetReset.Earliest,
        };

        using var consumer = new ConsumerBuilder<string, string>(config)
            .SetErrorHandler((_, error) =>
                _logger.LogWarning("Kafka consumer error: {Reason} (fatal: {IsFatal})", error.Reason, error.IsFatal))
            .Build();

        consumer.Subscribe(_options.TransactionsTopic);
        _logger.LogInformation(
            "Consuming topic {Topic} as group {GroupId}",
            _options.TransactionsTopic,
            _options.ConsumerGroupId);

        var attempts = 0;
        TopicPartitionOffset? lastFailed = null;

        while (!stoppingToken.IsCancellationRequested)
        {
            ConsumeResult<string, string>? result = null;
            try
            {
                result = consumer.Consume(stoppingToken);
                if (result?.Message is null)
                {
                    continue;
                }

                attempts = result.TopicPartitionOffset == lastFailed ? attempts + 1 : 1;

                await ProcessAsync(result, stoppingToken);
                consumer.Commit(result);
                lastFailed = null;
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (ConsumeException ex)
            {
                _logger.LogError(ex, "Failed to consume from {Topic}", _options.TransactionsTopic);
                await Task.Delay(RetryDelay, stoppingToken);
            }
            catch (JsonException ex) when (result is not null)
            {
                // Poison message: it can never be parsed, so retrying is pointless.
                await DeadLetterAsync(result, $"Malformed payload: {ex.Message}", stoppingToken);
                consumer.Commit(result);
                lastFailed = null;
            }
            catch (Exception ex) when (result is not null)
            {
                if (attempts >= MaxDeliveryAttempts)
                {
                    _logger.LogError(
                        ex,
                        "Giving up on message at {Offset} after {Attempts} attempts; dead-lettering",
                        result.TopicPartitionOffset,
                        attempts);
                    await DeadLetterAsync(result, $"Failed after {attempts} attempts: {ex.Message}", stoppingToken);
                    consumer.Commit(result);
                    lastFailed = null;
                }
                else
                {
                    _logger.LogWarning(
                        ex,
                        "Processing failed for message at {Offset} (attempt {Attempt}); retrying",
                        result.TopicPartitionOffset,
                        attempts);
                    lastFailed = result.TopicPartitionOffset;
                    consumer.Seek(result.TopicPartitionOffset);
                    await Task.Delay(RetryDelay, stoppingToken);
                }
            }
        }

        consumer.Close();
        _logger.LogInformation("Transaction events consumer stopped");
    }

    private async Task ProcessAsync(ConsumeResult<string, string> result, CancellationToken cancellationToken)
    {
        var transaction = JsonSerializer.Deserialize<TransactionResponse>(result.Message.Value, SerializerOptions)
            ?? throw new JsonException("Event payload deserialized to null.");

        using var scope = _logger.BeginScope(new Dictionary<string, object?>
        {
            ["CorrelationId"] = ReadCorrelationId(result.Message.Headers),
        });

        await using var serviceScope = _scopeFactory.CreateAsyncScope();
        var db = serviceScope.ServiceProvider.GetRequiredService<CreditCardDbContext>();

        db.ConsumedTransactionEvents.Add(new ConsumedTransactionEvent
        {
            TransactionId = transaction.Id,
            ConsumedAt = _clock.GetUtcNow().UtcDateTime,
        });

        try
        {
            await db.SaveChangesAsync(cancellationToken);
            _logger.LogInformation(
                "Processed transaction event {TransactionId} from {Offset}",
                transaction.Id,
                result.TopicPartitionOffset);
        }
        catch (DbUpdateException ex) when (
            ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            // Duplicate delivery (at-least-once): the ledger row already exists, so skip quietly.
            _logger.LogInformation(
                "Skipped duplicate transaction event {TransactionId} from {Offset}",
                transaction.Id,
                result.TopicPartitionOffset);
        }
    }

    private async Task DeadLetterAsync(
        ConsumeResult<string, string> result,
        string reason,
        CancellationToken cancellationToken)
    {
        var message = new Message<string, string>
        {
            Key = result.Message.Key,
            Value = result.Message.Value,
            Headers = result.Message.Headers ?? [],
        };
        message.Headers.Add("x-dead-letter-reason", Encoding.UTF8.GetBytes(reason));
        message.Headers.Add("x-original-offset", Encoding.UTF8.GetBytes(result.TopicPartitionOffset.ToString()));

        // If this produce fails the exception bubbles up, the offset stays uncommitted and the
        // message is redelivered — dead-lettering never loses data.
        await _producer.ProduceAsync(_options.DeadLetterTopic, message, cancellationToken);

        _logger.LogWarning(
            "Dead-lettered message from {Offset} to {DeadLetterTopic}: {Reason}",
            result.TopicPartitionOffset,
            _options.DeadLetterTopic,
            reason);
    }

    private static string? ReadCorrelationId(Headers? headers)
    {
        if (headers is not null && headers.TryGetLastBytes(Correlation.HeaderName, out var bytes))
        {
            return Encoding.UTF8.GetString(bytes);
        }

        return null;
    }
}
