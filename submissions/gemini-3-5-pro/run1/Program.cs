using System;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using CreditCardApi.Infrastructure.Data;
using CreditCardApi.Infrastructure.Messaging;
using CreditCardApi.Repositories;
using CreditCardApi.UseCases.CreditCards;
using CreditCardApi.UseCases.Transactions;

var builder = WebApplication.CreateBuilder(args);

// Configure Controllers and JSON Serialization
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.DictionaryKeyPolicy = JsonNamingPolicy.CamelCase;
    });

// Configure PostgreSQL Database Context
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
    ?? "Host=postgres;Database=creditcarddb;Username=postgres;Password=postgres";
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

// Configure Kafka Producer Service
builder.Services.AddSingleton<IKafkaProducerService, KafkaProducerService>();

// Configure Repositories
builder.Services.AddScoped<ICreditCardRepository, CreditCardRepository>();
builder.Services.AddScoped<ITransactionRepository, TransactionRepository>();

// Configure CreditCard Use Cases
builder.Services.AddScoped<GetCreditCardsUseCase>();
builder.Services.AddScoped<GetCreditCardByIdUseCase>();
builder.Services.AddScoped<CreateCreditCardUseCase>();
builder.Services.AddScoped<UpdateCreditCardUseCase>();
builder.Services.AddScoped<DeleteCreditCardUseCase>();
builder.Services.AddScoped<GetCreditCardTransactionsUseCase>();

// Configure Transaction Use Cases
builder.Services.AddScoped<GetTransactionsUseCase>();
builder.Services.AddScoped<GetTransactionByIdUseCase>();
builder.Services.AddScoped<CreateTransactionUseCase>();
builder.Services.AddScoped<UpdateTransactionUseCase>();
builder.Services.AddScoped<DeleteTransactionUseCase>();

var app = builder.Build();

// Run Database Migrations on Startup with Retry Logic (Handles database boot readiness)
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();
    var context = services.GetRequiredService<AppDbContext>();

    int retryCount = 15;
    int delayMs = 3000;
    for (int i = 0; i < retryCount; i++)
    {
        try
        {
            logger.LogInformation("Attempting to migrate database (attempt {Count} of {Total})...", i + 1, retryCount);
            context.Database.Migrate();
            logger.LogInformation("Database migration completed successfully.");
            break;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Database migration failed on attempt {Count}.", i + 1);
            if (i == retryCount - 1)
            {
                logger.LogCritical("Database migration failed after max retries. Exiting.");
                throw;
            }
            System.Threading.Thread.Sleep(delayMs);
        }
    }
}

app.UseAuthorization();
app.MapControllers();

app.Run();
