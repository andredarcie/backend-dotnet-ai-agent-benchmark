using CreditCardApi.Data;
using CreditCardApi.Repositories;
using CreditCardApi.Services;
using CreditCardApi.UseCases.CreditCards;
using CreditCardApi.UseCases.Transactions;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(opts =>
    {
        opts.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    });

builder.Services.AddDbContext<AppDbContext>(opts =>
    opts.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Repositories
builder.Services.AddScoped<ICreditCardRepository, CreditCardRepository>();
builder.Services.AddScoped<ITransactionRepository, TransactionRepository>();

// Credit card use cases
builder.Services.AddScoped<GetAllCreditCardsUseCase>();
builder.Services.AddScoped<GetCreditCardByIdUseCase>();
builder.Services.AddScoped<CreateCreditCardUseCase>();
builder.Services.AddScoped<UpdateCreditCardUseCase>();
builder.Services.AddScoped<DeleteCreditCardUseCase>();
builder.Services.AddScoped<GetTransactionsByCardIdUseCase>();

// Transaction use cases
builder.Services.AddScoped<GetAllTransactionsUseCase>();
builder.Services.AddScoped<GetTransactionByIdUseCase>();
builder.Services.AddScoped<CreateTransactionUseCase>();
builder.Services.AddScoped<UpdateTransactionUseCase>();
builder.Services.AddScoped<DeleteTransactionUseCase>();

// Kafka
builder.Services.AddSingleton<IKafkaProducerService, KafkaProducerService>();

var app = builder.Build();

// Run migrations on startup with retry
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    for (int attempt = 1; attempt <= 10; attempt++)
    {
        try
        {
            await db.Database.MigrateAsync();
            logger.LogInformation("Database migrations applied successfully");
            break;
        }
        catch (Exception ex)
        {
            if (attempt == 10)
            {
                logger.LogError(ex, "Failed to apply migrations after {Attempts} attempts", attempt);
                throw;
            }
            logger.LogWarning("Migration attempt {Attempt} failed, retrying in {Delay}s...", attempt, attempt * 2);
            await Task.Delay(attempt * 2000);
        }
    }
}

app.MapControllers();

app.Run();
