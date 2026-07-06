using System;
using System.Threading;
using System.Threading.Tasks;
using Confluent.Kafka;
using Confluent.Kafka.Admin;
using CreditCardApi.Application.Common.Interfaces;
using CreditCardApi.Domain.Entities;
using CreditCardApi.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CreditCardApi.Infrastructure.Messaging;

/// <summary>
/// A reliable, idempotent Kafka consumer service with manual offset commit and a Dead-Letter Queue (DLQ).
/// </summary>
public class KafkaConsumerBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<KafkaConsumerBackgroundService> _logger;
    private readonly string _bootstrapServers;
    private const string TopicName = "transactions";
    private const string DqTopicName = "transactions-dlq";

    public KafkaConsumerBackgroundService(
        IServiceProvider serviceProvider,
        IConfiguration configuration,
        ILogger<KafkaConsumerBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _bootstrapServers = configuration["Kafka:BootstrapServers"] 
                            ?? configuration["Kafka__BootstrapServers"] 
                            ?? "kafka:9092";
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Run the consumer loop on a background thread so it doesn't block startup
        return Task.Run(() => StartConsumerLoop(stoppingToken), stoppingToken);
    }

    private async Task StartConsumerLoop(CancellationToken stoppingToken)
    {
        var config = new ConsumerConfig
        {
            BootstrapServers = _bootstrapServers,
            GroupId = "CreditCardApi-ConsumerGroup",
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false, // Critical: We manually commit offsets after processing
            ClientId = "CreditCardApi-Consumer"
        };

        // Create Kafka Topics automatically on consumer startup (failsafe helper)
        await EnsureTopicsCreatedAsync();

        using var consumer = new ConsumerBuilder<string, string>(config).Build();
        consumer.Subscribe(TopicName);
        _logger.LogInformation("Kafka consumer subscribed to topic: {Topic}. Group: {Group}", TopicName, config.GroupId);

        while (!stoppingToken.IsCancellationRequested)
        {
            ConsumeResult<string, string>? consumeResult = null;
            try
            {
                // Poll Kafka for new messages
                consumeResult = consumer.Consume(TimeSpan.FromSeconds(1));
                if (consumeResult == null)
                    continue;

                var messageKey = consumeResult.Message.Key ?? Guid.NewGuid().ToString();
                _logger.LogInformation("Received message from Kafka. Key: {Key}, Topic: {Topic}", messageKey, consumeResult.Topic);

                // Process the message idempotently
                var successfullyProcessed = await ProcessMessageIdempotentlyAsync(messageKey, consumeResult.Message.Value, stoppingToken);

                if (successfullyProcessed)
                {
                    // Commit offset manually after processing succeeds
                    consumer.Commit(consumeResult);
                    _logger.LogInformation("Successfully processed message and committed offset. Key: {Key}", messageKey);
                }
                else
                {
                    // If parsing or business logic failed, route to DLQ
                    _logger.LogWarning("Message processing failed. Routing to Dead-Letter Queue. Key: {Key}", messageKey);
                    await RouteToDlqAsync(messageKey, consumeResult.Message.Value, stoppingToken);
                    consumer.Commit(consumeResult); // Commit offset so we don't get stuck in an infinite loop
                }
            }
            catch (ConsumeException ex)
            {
                _logger.LogError(ex, "Error occurred during Kafka consumption.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fatal error in consumer background service loop.");
                if (consumeResult != null)
                {
                    // Attempt to route raw failed message to DLQ
                    await RouteToDlqAsync(consumeResult.Message.Key ?? "Unknown", consumeResult.Message.Value, stoppingToken);
                    try
                    {
                        consumer.Commit(consumeResult);
                    }
                    catch
                    {
                        // Ignore commit error on fatal failure
                    }
                }
                // Short sleep to prevent CPU spinning on persistent errors
                await Task.Delay(1000, stoppingToken);
            }
        }

        consumer.Close();
    }

    private async Task<bool> ProcessMessageIdempotentlyAsync(string messageId, string value, CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // 1. Deduplication check (Idempotent Consumer pattern)
        var alreadyProcessed = await context.ProcessedConsumerMessages
            .AnyAsync(m => m.MessageId == messageId, cancellationToken);

        if (alreadyProcessed)
        {
            _logger.LogInformation("Duplicate message detected. Skipping processing. Key: {Key}", messageId);
            return true; // Return true because it is already processed (idempotency holds)
        }

        // 2. Perform message processing logic
        try
        {
            // Simulate processing transaction (e.g. auditing, risk scoring, emailing etc.)
            _logger.LogInformation("Idempotent Consumer is processing event payload: {Payload}", value);

            // 3. Mark as processed in the database inside the same transaction
            var processedMessage = new ProcessedConsumerMessage(messageId);
            context.ProcessedConsumerMessages.Add(processedMessage);
            await context.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process message payload for Key: {Key}", messageId);
            return false;
        }
    }

    private async Task RouteToDlqAsync(string key, string value, CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var producer = scope.ServiceProvider.GetRequiredService<IKafkaProducer>();
            
            // Append a failure header or indicator if necessary, or just route to DLQ topic
            await producer.PublishAsync(DqTopicName, key, value, cancellationToken);
            _logger.LogInformation("Successfully sent message to DLQ: {Topic}, Key: {Key}", DqTopicName, key);
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Failed to publish message to DLQ! Message content: {Value}", value);
        }
    }

    private async Task EnsureTopicsCreatedAsync()
    {
        try
        {
            using var adminClient = new AdminClientBuilder(new AdminClientConfig { BootstrapServers = _bootstrapServers }).Build();
            
            // Create main and DLQ topics
            var topicSpecs = new[]
            {
                new TopicSpecification { Name = TopicName, NumPartitions = 1, ReplicationFactor = 1 },
                new TopicSpecification { Name = DqTopicName, NumPartitions = 1, ReplicationFactor = 1 }
            };

            await adminClient.CreateTopicsAsync(topicSpecs);
            _logger.LogInformation("Kafka topics verified/created successfully.");
        }
        catch (CreateTopicsException ex)
        {
            // If the topics already exist, it is normal and expected
            foreach (var result in ex.Results)
            {
                if (result.Error.Code != ErrorCode.TopicAlreadyExists)
                {
                    _logger.LogWarning("Error auto-creating Kafka topic: {Topic}, Reason: {Reason}", 
                        result.Topic, result.Error.Reason);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to check or auto-create Kafka topics on startup. Assuming Kafka creates them automatically.");
        }
    }
}
