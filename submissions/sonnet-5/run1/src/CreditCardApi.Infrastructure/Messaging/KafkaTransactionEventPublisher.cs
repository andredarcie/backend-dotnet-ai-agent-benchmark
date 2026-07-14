using System.Globalization;
using System.Text.Json;
using Confluent.Kafka;
using CreditCardApi.Application.Transactions;
using CreditCardApi.Application.Transactions.Dtos;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using Polly.Timeout;

namespace CreditCardApi.Infrastructure.Messaging;

/// <summary>
/// Durable Kafka producer for the "transaction created" event. Acks=all + idempotence + broker-side
/// retries (configured on the injected <see cref="IProducer{TKey,TValue}"/>, see
/// <see cref="DependencyInjection"/>) make an individual send durable; the resilience pipeline on
/// top absorbs transient broker hiccups. If publishing still fails after retries, the failure is
/// logged and swallowed - the HTTP request that already persisted the transaction must not fail
/// because of it.
/// </summary>
public sealed class KafkaTransactionEventPublisher : ITransactionEventPublisher
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IProducer<string, string> _producer;
    private readonly KafkaOptions _options;
    private readonly ILogger<KafkaTransactionEventPublisher> _logger;
    private readonly ResiliencePipeline _resiliencePipeline;

    public KafkaTransactionEventPublisher(
        IProducer<string, string> producer,
        IOptions<KafkaOptions> options,
        ILogger<KafkaTransactionEventPublisher> logger)
    {
        _producer = producer;
        _options = options.Value;
        _logger = logger;

        _resiliencePipeline = new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions
            {
                MaxRetryAttempts = 3,
                BackoffType = DelayBackoffType.Exponential,
                Delay = TimeSpan.FromMilliseconds(200),
            })
            .AddCircuitBreaker(new CircuitBreakerStrategyOptions
            {
                FailureRatio = 0.5,
                MinimumThroughput = 4,
                SamplingDuration = TimeSpan.FromSeconds(30),
                BreakDuration = TimeSpan.FromSeconds(15),
            })
            .AddTimeout(TimeSpan.FromSeconds(5))
            .Build();
    }

    public async Task PublishCreatedAsync(TransactionResponse transaction, CancellationToken cancellationToken)
    {
        var message = new Message<string, string>
        {
            Key = transaction.Id.ToString(CultureInfo.InvariantCulture),
            Value = JsonSerializer.Serialize(transaction, JsonOptions),
        };

        try
        {
            await _resiliencePipeline.ExecuteAsync(
                async ct => await _producer.ProduceAsync(_options.TransactionsTopic, message, ct),
                cancellationToken);
        }
        catch (Exception ex)
        {
            // Deliberately broad: whatever the failure mode (a typed Kafka/Polly exception or
            // anything else), the row is already committed and the request must not fail because
            // the event couldn't be published.
            _logger.LogError(ex,
                "Failed to publish transaction {TransactionId} to Kafka topic '{Topic}' after retries; the transaction was already persisted.",
                transaction.Id, _options.TransactionsTopic);
        }
    }
}
