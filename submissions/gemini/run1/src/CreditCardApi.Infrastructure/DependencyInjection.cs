using System;
using CreditCardApi.Application.Common.Interfaces;
using CreditCardApi.Infrastructure.Messaging;
using CreditCardApi.Infrastructure.Persistence;
using CreditCardApi.Infrastructure.Persistence.Repositories;
using CreditCardApi.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CreditCardApi.Infrastructure;

/// <summary>
/// Service collection extension for registering Infrastructure layer services.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // 1. AppDbContext with PostgreSQL configuration & automatic connection resilience (EF Core built-in retries)
        var connectionString = configuration.GetConnectionString("DefaultConnection")
                               ?? configuration["ConnectionStrings__DefaultConnection"]
                               ?? configuration["DbConnectionString"]
                               ?? configuration["DATABASE_URL"]
                               ?? "Host=localhost;Port=5432;Database=creditcarddb;Username=postgres;Password=postgres";

        services.AddDbContext<AppDbContext>(options =>
        {
            options.UseNpgsql(connectionString, npgsqlOptions =>
            {
                // DB resilience: retry on transient PostgreSQL errors
                npgsqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 5,
                    maxRetryDelay: TimeSpan.FromSeconds(10),
                    errorCodesToAdd: null);
            });
        });

        // 2. Encryption and Security
        services.AddSingleton<ICardEncryptionService, CardEncryptionService>();

        // 3. Repositories
        services.AddScoped<ICreditCardRepository, CreditCardRepository>();
        services.AddScoped<ITransactionRepository, TransactionRepository>();
        services.AddScoped<IUnitOfWork>(provider => provider.GetRequiredService<AppDbContext>());

        // 4. Kafka Messaging Components
        services.AddSingleton<IKafkaProducer, KafkaProducer>();
        
        // Background workers
        services.AddHostedService<OutboxProcessorBackgroundService>();
        services.AddHostedService<KafkaConsumerBackgroundService>();

        return services;
    }
}
