using CreditCardApi.Data;
using CreditCardApi.Infrastructure.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CreditCardApi.Infrastructure.Services;

public class OutboxPublisherService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<OutboxPublisherService> _logger;
    private readonly TimeSpan _pollInterval = TimeSpan.FromSeconds(5);

    public OutboxPublisherService(
        IServiceProvider serviceProvider,
        ILogger<OutboxPublisherService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Outbox publisher service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var kafkaProducer = scope.ServiceProvider.GetRequiredService<IKafkaProducer>();

                var unprocessedEvents = await context.OutboxEvents
                    .Where(e => e.ProcessedAt == null)
                    .OrderBy(e => e.CreatedAt)
                    .Take(10)
                    .ToListAsync(stoppingToken);

                foreach (var @event in unprocessedEvents)
                {
                    try
                    {
                        await kafkaProducer.PublishAsync(
                            @event.Topic,
                            @event.Key,
                            @event.Payload,
                            stoppingToken);

                        @event.ProcessedAt = DateTime.UtcNow;
                        context.OutboxEvents.Update(@event);
                        await context.SaveChangesAsync(stoppingToken);

                        _logger.LogInformation(
                            "Published outbox event {EventId} to topic {Topic}",
                            @event.Id, @event.Topic);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(
                            ex,
                            "Failed to publish outbox event {EventId}",
                            @event.Id);
                    }
                }

                await Task.Delay(_pollInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Outbox publisher service stopping");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in outbox publisher service");
                await Task.Delay(_pollInterval, stoppingToken);
            }
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Outbox publisher service stopping");
        await base.StopAsync(cancellationToken);
    }
}
