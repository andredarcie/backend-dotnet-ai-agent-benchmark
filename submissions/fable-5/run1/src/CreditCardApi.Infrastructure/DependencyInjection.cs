using Confluent.Kafka;
using CreditCardApi.Application.Abstractions;
using CreditCardApi.Infrastructure.Messaging;
using CreditCardApi.Infrastructure.Messaging.Consuming;
using CreditCardApi.Infrastructure.Messaging.Outbox;
using CreditCardApi.Infrastructure.Persistence;
using CreditCardApi.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CreditCardApi.Infrastructure;

/// <summary>Composition of the infrastructure layer: persistence and messaging.</summary>
public static class DependencyInjection
{
    /// <summary>Registers EF Core, repositories, the outbox, and the Kafka clients/workers.</summary>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        AddPersistence(services, configuration);
        AddMessaging(services, configuration);
        return services;
    }

    private static void AddPersistence(IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException(
                "Connection string 'Default' is not configured. Set the ConnectionStrings__Default environment variable.");

        services.AddDbContext<CreditCardDbContext>(options => options.UseNpgsql(
            connectionString,
            npgsql => npgsql.EnableRetryOnFailure(
                maxRetryCount: 5,
                maxRetryDelay: TimeSpan.FromSeconds(10),
                errorCodesToAdd: null)));

        services.AddScoped<ICreditCardRepository, CreditCardRepository>();
        services.AddScoped<ITransactionRepository, TransactionRepository>();
        services.AddScoped<IUnitOfWork, EfUnitOfWork>();
    }

    private static void AddMessaging(IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<KafkaOptions>()
            .Bind(configuration.GetSection(KafkaOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddSingleton<IProducer<string, string>>(provider =>
        {
            var options = provider.GetRequiredService<IOptions<KafkaOptions>>().Value;
            var logger = provider.GetRequiredService<ILogger<OutboxDispatcher>>();

            // Durable producer: wait for all in-sync replicas and deduplicate broker-side retries.
            var config = new ProducerConfig
            {
                BootstrapServers = options.BootstrapServers,
                Acks = Acks.All,
                EnableIdempotence = true,
                LingerMs = 5,
                MessageTimeoutMs = 10_000,
            };

            return new ProducerBuilder<string, string>(config)
                .SetErrorHandler((_, error) =>
                    logger.LogWarning("Kafka producer error: {Reason} (fatal: {IsFatal})", error.Reason, error.IsFatal))
                .Build();
        });

        services.AddSingleton<IAdminClient>(provider =>
        {
            var options = provider.GetRequiredService<IOptions<KafkaOptions>>().Value;
            return new AdminClientBuilder(new AdminClientConfig
            {
                BootstrapServers = options.BootstrapServers,
            }).Build();
        });

        services.AddScoped<ITransactionEventPublisher, OutboxTransactionEventPublisher>();
        services.AddHostedService<OutboxDispatcher>();
        services.AddHostedService<TransactionEventsConsumer>();
    }
}
