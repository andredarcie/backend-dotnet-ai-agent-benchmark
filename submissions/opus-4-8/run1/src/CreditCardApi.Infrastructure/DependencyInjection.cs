using CreditCardApi.Application.Abstractions;
using CreditCardApi.Application.CreditCards;
using CreditCardApi.Application.Transactions;
using CreditCardApi.Infrastructure.Messaging;
using CreditCardApi.Infrastructure.Persistence;
using CreditCardApi.Infrastructure.Persistence.Repositories;
using CreditCardApi.Infrastructure.Security;
using CreditCardApi.Infrastructure.Time;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CreditCardApi.Infrastructure;

/// <summary>Wires the application use cases and all infrastructure into the DI container.</summary>
public static class DependencyInjection
{
    /// <summary>Registers persistence, messaging, security and the application services.</summary>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        AddOptions(services, configuration);
        AddPersistence(services, configuration);
        AddMessaging(services);
        AddApplicationServices(services);

        services.AddSingleton<IClock, SystemClock>();
        services.AddSingleton<IPanProtector, AesPanProtector>();

        return services;
    }

    private static void AddOptions(IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<KafkaOptions>(configuration.GetSection(KafkaOptions.SectionName));
        services.Configure<OutboxOptions>(configuration.GetSection(OutboxOptions.SectionName));
        services.Configure<PanProtectionOptions>(configuration.GetSection(PanProtectionOptions.SectionName));
    }

    private static void AddPersistence(IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException(
                "Connection string 'Default' is not configured (set ConnectionStrings__Default).");

        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(connectionString, npgsql =>
            {
                npgsql.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName);
                npgsql.EnableRetryOnFailure(maxRetryCount: 5, maxRetryDelay: TimeSpan.FromSeconds(5), errorCodesToAdd: null);
                npgsql.CommandTimeout(30);
            }));

        services.AddScoped<ICreditCardRepository, CreditCardRepository>();
        services.AddScoped<ITransactionRepository, TransactionRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IIntegrationEventPublisher, OutboxEventPublisher>();
    }

    private static void AddMessaging(IServiceCollection services)
    {
        services.AddSingleton<KafkaProducer>();
        services.AddHostedService<KafkaTopicInitializer>();
        services.AddHostedService<OutboxDispatcher>();
        services.AddHostedService<TransactionConsumer>();
    }

    private static void AddApplicationServices(IServiceCollection services)
    {
        services.AddScoped<CreditCardService>();
        services.AddScoped<TransactionService>();
    }
}
