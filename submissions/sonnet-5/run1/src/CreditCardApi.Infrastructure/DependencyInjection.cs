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
using Microsoft.Extensions.Options;

namespace CreditCardApi.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Read the connection string lazily (inside the options callback, evaluated when the DbContext is
        // first resolved) rather than eagerly here: WebApplicationFactory-based tests inject their
        // Testcontainers connection string into IConfiguration after this method runs but before the
        // host finishes building, so an eager read here would miss it.
        services.AddDbContext<CreditCardDbContext>(options =>
        {
            var connectionString = configuration.GetConnectionString("Default")
                ?? throw new InvalidOperationException("Connection string 'ConnectionStrings:Default' is not configured.");
            options.UseNpgsql(connectionString, npgsql => npgsql.EnableRetryOnFailure(5, TimeSpan.FromSeconds(10), null))
                .UseSnakeCaseNamingConvention();
        });

        services.AddScoped<IUnitOfWork, EfUnitOfWork>();
        services.AddScoped<ICreditCardRepository, CreditCardRepository>();
        services.AddScoped<ITransactionRepository, TransactionRepository>();
        services.AddScoped<ITransactionEventPublisher, OutboxTransactionEventPublisher>();
        services.AddScoped<DatabaseInitializer>();

        services.AddOptions<KafkaOptions>()
            .Bind(configuration.GetSection(KafkaOptions.SectionName));

        services.AddSingleton<IProducer<string, string>>(sp =>
        {
            var kafkaOptions = sp.GetRequiredService<IOptions<KafkaOptions>>().Value;
            var producerConfig = new ProducerConfig
            {
                BootstrapServers = kafkaOptions.BootstrapServers,
                Acks = Acks.All,
                EnableIdempotence = true,
                MessageSendMaxRetries = 5,
                RetryBackoffMs = 500,
            };
            return new ProducerBuilder<string, string>(producerConfig).Build();
        });

        services.AddSingleton<KafkaTopicInitializer>();
        services.AddHostedService<OutboxDispatcher>();
        services.AddHostedService<TransactionEventsConsumer>();

        return services;
    }
}
