using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Gemini.Data;
using Gemini.Data.Repositories;
using Gemini.Messaging;
using Gemini.UseCases;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var builder = WebApplication.CreateBuilder(args);

// Configure Web Host to listen on port 8080
builder.WebHost.UseUrls("http://*:8080");

// Add services to the container.
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.Never;
    });

// Configure EF Core with PostgreSQL
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Register Repository Layer
builder.Services.AddScoped<ICreditCardRepository, CreditCardRepository>();
builder.Services.AddScoped<ITransactionRepository, TransactionRepository>();

// Register Messaging Layer
builder.Services.AddSingleton<IKafkaProducer, KafkaProducer>();

// Register Use Cases (Application Layer)
builder.Services.AddScoped<GetAllCreditCardsUseCase>();
builder.Services.AddScoped<GetCreditCardByIdUseCase>();
builder.Services.AddScoped<CreateCreditCardUseCase>();
builder.Services.AddScoped<UpdateCreditCardUseCase>();
builder.Services.AddScoped<DeleteCreditCardUseCase>();
builder.Services.AddScoped<GetTransactionsByCreditCardIdUseCase>();

builder.Services.AddScoped<GetAllTransactionsUseCase>();
builder.Services.AddScoped<GetTransactionByIdUseCase>();
builder.Services.AddScoped<CreateTransactionUseCase>();
builder.Services.AddScoped<UpdateTransactionUseCase>();
builder.Services.AddScoped<DeleteTransactionUseCase>();

var app = builder.Build();

// Database Schema Auto-Creation on Startup with Retries (Postgres startup wait)
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();
    var context = services.GetRequiredService<AppDbContext>();

    int maxRetries = 10;
    int retryDelaySeconds = 3;

    for (int i = 1; i <= maxRetries; i++)
    {
        try
        {
            logger.LogInformation("Database connection attempt {Attempt}/{Max}", i, maxRetries);
            context.Database.EnsureCreated();
            logger.LogInformation("Database and tables created/verified successfully.");
            break;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to connect to the database on attempt {Attempt}. Retrying in {Delay} seconds...", i, retryDelaySeconds);
            if (i == maxRetries)
            {
                logger.LogError(ex, "Could not initialize database. Maximum retries reached.");
                throw;
            }
            System.Threading.Thread.Sleep(TimeSpan.FromSeconds(retryDelaySeconds));
        }
    }
}

app.UseAuthorization();
app.MapControllers();

app.Run();
