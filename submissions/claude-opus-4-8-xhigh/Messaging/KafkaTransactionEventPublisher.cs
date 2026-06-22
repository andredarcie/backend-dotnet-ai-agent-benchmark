using System.Text.Json;
using Confluent.Kafka;
using CreditCardApi.DTOs;
using Microsoft.Extensions.Options;

namespace CreditCardApi.Messaging;

/// <summary>
/// Confluent.Kafka producer. Registered as a singleton — the underlying producer is
/// thread-safe and designed to be reused across the whole application lifetime.
/// </summary>
public sealed class KafkaTransactionEventPublisher : ITransactionEventPublisher, IDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IProducer<string, string> _producer;
    private readonly KafkaSettings _settings;
    private readonly ILogger<KafkaTransactionEventPublisher> _logger;

    public KafkaTransactionEventPublisher(
        IOptions<KafkaSettings> options,
        ILogger<KafkaTransactionEventPublisher> logger)
    {
        _settings = options.Value;
        _logger = logger;

        var config = new ProducerConfig
        {
            BootstrapServers = _settings.BootstrapServers,
            Acks = Acks.All,
            EnableIdempotence = true,
            AllowAutoCreateTopics = true,
            MessageTimeoutMs = 10000,
            // Keep the request path responsive even if the broker is briefly unreachable.
            SocketTimeoutMs = 10000
        };

        _producer = new ProducerBuilder<string, string>(config)
            .SetErrorHandler((_, error) =>
                _logger.LogWarning("Kafka producer error: {Reason}", error.Reason))
            .Build();
    }

    public async Task PublishTransactionCreatedAsync(TransactionResponse transaction, CancellationToken ct = default)
    {
        try
        {
            var message = new Message<string, string>
            {
                Key = transaction.Id.ToString(),
                Value = JsonSerializer.Serialize(transaction, JsonOptions)
            };

            await _producer.ProduceAsync(_settings.TransactionsTopic, message, ct);

            _logger.LogInformation(
                "Published transaction {Id} to topic {Topic}.",
                transaction.Id,
                _settings.TransactionsTopic);
        }
        catch (Exception ex)
        {
            // Never fail the HTTP request because of a messaging hiccup — the transaction
            // is already persisted. Log so the failure is observable.
            _logger.LogError(ex, "Failed to publish transaction {Id} to Kafka.", transaction.Id);
        }
    }

    public void Dispose()
    {
        try
        {
            _producer.Flush(TimeSpan.FromSeconds(5));
        }
        catch
        {
            // Best-effort flush on shutdown.
        }

        _producer.Dispose();
    }
}
