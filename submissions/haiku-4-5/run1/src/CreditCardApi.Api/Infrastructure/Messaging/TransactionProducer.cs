using System.Text.Json;
using CreditCardApi.Api.Application.Dto;
using Confluent.Kafka;

namespace CreditCardApi.Api.Infrastructure.Messaging;

public interface ITransactionProducer
{
    Task PublishTransactionAsync(TransactionDto transaction, CancellationToken cancellationToken = default);
}

public class TransactionProducer : ITransactionProducer, IAsyncDisposable
{
    private readonly IProducer<string, string> _producer;
    private readonly ILogger<TransactionProducer> _logger;

    public TransactionProducer(IConfiguration configuration, ILogger<TransactionProducer> logger)
    {
        _logger = logger;
        var bootstrapServers = configuration["Kafka:BootstrapServers"] ?? "kafka:9092";

        var config = new ProducerConfig
        {
            BootstrapServers = bootstrapServers,
            Acks = Acks.All,
            MessageSendMaxRetries = 3,
            SocketTimeoutMs = 60000,
            RequestTimeoutMs = 60000,
            RetryBackoffMs = 100,
            ClientId = "creditcard-api",
            EnableDeliveryReports = true
        };

        _producer = new ProducerBuilder<string, string>(config).Build();
    }

    public async Task PublishTransactionAsync(TransactionDto transaction, CancellationToken cancellationToken = default)
    {
        try
        {
            var json = JsonSerializer.Serialize(transaction, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
            var key = transaction.Id.ToString();

            var message = new Message<string, string>
            {
                Key = key,
                Value = json
            };

            await _producer.ProduceAsync("transactions", message, cancellationToken);

            _logger.LogInformation(
                "Published transaction {TransactionId} to Kafka topic 'transactions'",
                transaction.Id);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Kafka publish cancelled for transaction {TransactionId}", transaction.Id);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error publishing transaction {TransactionId} to Kafka", transaction.Id);
            throw;
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_producer != null)
        {
            _producer.Flush(TimeSpan.FromSeconds(10));
            _producer.Dispose();
        }
        await ValueTask.CompletedTask;
    }
}
