using System.Text.Json;
using Confluent.Kafka;
using CreditCardApi.Application.Abstractions;
using CreditCardApi.Application.Services;
using CreditCardApi.Data;
using CreditCardApi.Infrastructure.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Polly;
using Polly.CircuitBreaker;

namespace CreditCardApi.Infrastructure.Messaging;

public sealed class OutboxPublisherService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IProducer<string, string> _producer;
    private readonly IOptions<KafkaOptions> _options;
    private readonly IClock _clock;
    private readonly ILogger<OutboxPublisherService> _logger;
    private readonly IAsyncPolicy _kafkaPolicy;

    public OutboxPublisherService(
        IServiceScopeFactory scopeFactory,
        IProducer<string, string> producer,
        IOptions<KafkaOptions> options,
        IClock clock,
        ILogger<OutboxPublisherService> logger)
    {
        _scopeFactory = scopeFactory;
        _producer = producer;
        _options = options;
        _clock = clock;
        _logger = logger;
        _kafkaPolicy = Policy
            .Handle<ProduceException<string, string>>()
            .Or<KafkaException>()
            .WaitAndRetryAsync(3, attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)))
            .WrapAsync(Policy
                .Handle<ProduceException<string, string>>()
                .Or<KafkaException>()
                .CircuitBreakerAsync(5, TimeSpan.FromSeconds(30)));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(_options.Value.OutboxPollSeconds));
        while (!stoppingToken.IsCancellationRequested)
        {
            await PublishBatchAsync(stoppingToken);
            await timer.WaitForNextTickAsync(stoppingToken);
        }
    }

    private async Task PublishBatchAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var messages = await dbContext.OutboxMessages
            .Where(message => message.ProcessedAt == null)
            .OrderBy(message => message.OccurredAt)
            .Take(25)
            .ToListAsync(cancellationToken);

        foreach (var message in messages)
        {
            try
            {
                if (message.Attempts >= _options.Value.MaxOutboxAttempts)
                {
                    await PublishDeadLetterAsync(message.MessageKey, message.Payload, message.LastError ?? "Outbox attempts exhausted", cancellationToken);
                    message.MarkDeadLettered(_clock.UtcNow);
                }
                else
                {
                    await _kafkaPolicy.ExecuteAsync(token => _producer.ProduceAsync(
                        _options.Value.TransactionsTopic,
                        new Message<string, string> { Key = message.MessageKey, Value = message.Payload },
                        token), cancellationToken);

                    message.MarkProcessed(_clock.UtcNow);
                }

                await dbContext.SaveChangesAsync(cancellationToken);
            }
            catch (BrokenCircuitException exception)
            {
                _logger.LogWarning(exception, "Kafka outbox circuit is open");
                message.MarkFailed(exception.Message);
                await dbContext.SaveChangesAsync(cancellationToken);
                return;
            }
            catch (ProduceException<string, string> exception)
            {
                _logger.LogWarning(exception, "Kafka delivery failed for outbox message {OutboxMessageId}", message.Id);
                message.MarkFailed(exception.Error.Reason);
                await dbContext.SaveChangesAsync(cancellationToken);
            }
            catch (KafkaException exception)
            {
                _logger.LogWarning(exception, "Kafka publish failed for outbox message {OutboxMessageId}", message.Id);
                message.MarkFailed(exception.Message);
                await dbContext.SaveChangesAsync(cancellationToken);
            }
        }
    }

    private Task PublishDeadLetterAsync(string key, string value, string error, CancellationToken cancellationToken)
    {
        var envelope = new DeadLetterEnvelope(_options.Value.TransactionsTopic, key, value, error, _clock.UtcNow);
        return _producer.ProduceAsync(
            _options.Value.DeadLetterTopic,
            new Message<string, string>
            {
                Key = key,
                Value = JsonSerializer.Serialize(envelope, JsonSerializationDefaults.CamelCase)
            },
            cancellationToken);
    }
}

