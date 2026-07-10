namespace CreditCardApi.Api.Infrastructure.Messaging;

using CreditCardApi.Api.Application.Dto;
using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using System.Text.Json;

public interface ITransactionProducer
{
    Task PublishTransactionCreatedAsync(TransactionResponse transaction, CancellationToken cancellationToken);
}

public class TransactionProducer : ITransactionProducer
{
    private readonly IProducer<string, string> _producer;
    private readonly ILogger<TransactionProducer> _logger;

    public TransactionProducer(KafkaProducerConfig config, ILogger<TransactionProducer> logger)
    {
        _logger = logger;
        var producerConfig = new ProducerConfig
        {
            BootstrapServers = config.BootstrapServers,
            Acks = Acks.All,
            MessageTimeoutMs = 10000,
        };

        _producer = new ProducerBuilder<string, string>(producerConfig)
            .SetErrorHandler((producer, error) =>
            {
                if (!error.IsFatal)
                {
                    _logger.LogWarning("Non-fatal Kafka error: {ErrorCode} - {ErrorReason}", error.Code, error.Reason);
                }
                else
                {
                    _logger.LogError("Fatal Kafka error: {ErrorCode} - {ErrorReason}", error.Code, error.Reason);
                }
            })
            .Build();
    }

    public async Task PublishTransactionCreatedAsync(TransactionResponse transaction, CancellationToken cancellationToken)
    {
        try
        {
            var json = JsonSerializer.Serialize(transaction, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            });

            var message = new Message<string, string>
            {
                Key = transaction.Id.ToString(),
                Value = json,
            };

            var result = await _producer.ProduceAsync("transactions", message, cancellationToken);
            _logger.LogInformation("Published transaction {TransactionId} to Kafka topic 'transactions' at offset {Offset}", transaction.Id, result.Offset);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Publishing transaction {TransactionId} was cancelled", transaction.Id);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error publishing transaction {TransactionId} to Kafka", transaction.Id);
            throw;
        }
    }
}

public class KafkaProducerConfig
{
    public string BootstrapServers { get; set; } = "kafka:9092";
}
