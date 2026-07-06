using System.Text.Json;
using Confluent.Kafka;
using CreditCardApi.Application.Abstractions;
using CreditCardApi.Application.Dtos;
using CreditCardApi.Application.Services;
using CreditCardApi.Data;
using CreditCardApi.Data.Entities;
using CreditCardApi.Infrastructure.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace CreditCardApi.Infrastructure.Messaging;

public sealed class TransactionConsumerService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IProducer<string, string> _producer;
    private readonly IOptions<KafkaOptions> _options;
    private readonly IClock _clock;
    private readonly ILogger<TransactionConsumerService> _logger;

    public TransactionConsumerService(
        IServiceScopeFactory scopeFactory,
        IProducer<string, string> producer,
        IOptions<KafkaOptions> options,
        IClock clock,
        ILogger<TransactionConsumerService> logger)
    {
        _scopeFactory = scopeFactory;
        _producer = producer;
        _options = options;
        _clock = clock;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var config = new ConsumerConfig
        {
            BootstrapServers = _options.Value.BootstrapServers,
            GroupId = _options.Value.ConsumerGroupId,
            EnableAutoCommit = false,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            ClientId = "credit-card-api-consumer",
            IsolationLevel = IsolationLevel.ReadCommitted,
            SessionTimeoutMs = 10_000
        };

        using var consumer = new ConsumerBuilder<string, string>(config).Build();
        consumer.Subscribe(_options.Value.TransactionsTopic);

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

                await ProcessAsync(result, stoppingToken);
                consumer.Commit(result);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                consumer.Close();
                return;
            }
            catch (Exception exception) when (result is not null)
            {
                _logger.LogWarning(exception, "Failed to process Kafka transaction message with key {MessageKey}", result.Message.Key);
                await PublishDeadLetterAsync(result, exception, stoppingToken);
                consumer.Commit(result);
            }
            catch (ConsumeException exception)
            {
                _logger.LogWarning(exception, "Kafka consume failed");
            }
        }
    }

    private async Task ProcessAsync(ConsumeResult<string, string> result, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(result.Message.Key))
        {
            throw new InvalidOperationException("Kafka message key is required for idempotency.");
        }

        var transaction = JsonSerializer.Deserialize<TransactionResponse>(result.Message.Value, JsonSerializationDefaults.CamelCase)
            ?? throw new InvalidOperationException("Kafka message value was not a valid transaction.");

        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var alreadyProcessed = await dbContext.ProcessedMessages
            .AsNoTracking()
            .AnyAsync(message => message.MessageKey == result.Message.Key, cancellationToken);

        if (alreadyProcessed)
        {
            return;
        }

        dbContext.ProcessedMessages.Add(new ProcessedMessage(result.Message.Key, _options.Value.TransactionsTopic, _clock.UtcNow));
        await dbContext.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Processed transaction event {TransactionId}", transaction.Id);
    }

    private Task PublishDeadLetterAsync(ConsumeResult<string, string> result, Exception exception, CancellationToken cancellationToken)
    {
        var envelope = new DeadLetterEnvelope(
            _options.Value.TransactionsTopic,
            result.Message.Key,
            result.Message.Value,
            exception.Message,
            _clock.UtcNow);

        return _producer.ProduceAsync(
            _options.Value.DeadLetterTopic,
            new Message<string, string>
            {
                Key = result.Message.Key ?? Guid.NewGuid().ToString("N"),
                Value = JsonSerializer.Serialize(envelope, JsonSerializationDefaults.CamelCase)
            },
            cancellationToken);
    }
}
