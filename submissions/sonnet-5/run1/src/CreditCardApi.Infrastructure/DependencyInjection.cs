using Confluent.Kafka;
using CreditCardApi.Application.CreditCards;
using CreditCardApi.Application.Transactions;
using CreditCardApi.Infrastructure.HealthChecks;
using CreditCardApi.Infrastructure.Messaging;
using CreditCardApi.Infrastructure.Persistence;
using CreditCardApi.Infrastructure.Persistence.Repositories;
using CreditCardApi.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CreditCardApi.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // The connection string read happens inside this callback (not eagerly above), so it is
        // resolved lazily on first DbContext use rather than at host build time.
        services.AddDbContext<AppDbContext>(options =>
        {
            var connectionString = configuration.GetConnectionString("Default")
                ?? throw new InvalidOperationException("Connection string 'ConnectionStrings:Default' is not configured.");

            options.UseNpgsql(connectionString, npgsql =>
                npgsql.EnableRetryOnFailure(maxRetryCount: 5, maxRetryDelay: TimeSpan.FromSeconds(10), errorCodesToAdd: null));
        });

        services.AddOptions<KafkaOptions>()
            .Bind(configuration.GetSection(KafkaOptions.SectionName))
            .ValidateOnStart();

        services.AddOptions<SecurityOptions>()
            .Bind(configuration.GetSection(SecurityOptions.SectionName))
            .ValidateOnStart();

        services.AddSingleton<IPanProtector, AesPanProtector>();

        services.AddScoped<ICreditCardRepository, CreditCardRepository>();
        services.AddScoped<ITransactionRepository, TransactionRepository>();

        services.AddSingleton<IProducer<string, string>>(sp =>
        {
            var kafkaOptions = sp.GetRequiredService<IOptions<KafkaOptions>>().Value;
            var logger = sp.GetRequiredService<ILogger<KafkaTransactionEventPublisher>>();

            var producerConfig = new ProducerConfig
            {
                BootstrapServers = kafkaOptions.BootstrapServers,
                Acks = Acks.All,
                EnableIdempotence = true,
                MessageSendMaxRetries = 5,
                RetryBackoffMs = 200,
            };

            return new ProducerBuilder<string, string>(producerConfig)
                .SetErrorHandler((_, error) => logger.LogWarning("Kafka producer error ({Code}): {Reason}", error.Code, error.Reason))
                .Build();
        });
        services.AddSingleton<ITransactionEventPublisher, KafkaTransactionEventPublisher>();
        services.AddHostedService<KafkaTopicInitializer>();

        services.AddHealthChecks()
            .AddCheck<PostgresHealthCheck>("postgres", tags: ["ready"])
            .AddCheck<KafkaHealthCheck>("kafka", tags: ["ready"]);

        return services;
    }
}
