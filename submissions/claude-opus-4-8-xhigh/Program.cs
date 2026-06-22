using CreditCardApi.Data;
using CreditCardApi.Messaging;
using CreditCardApi.Repositories;
using CreditCardApi.UseCases.CreditCards;
using CreditCardApi.UseCases.Transactions;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// ---- MVC / Controllers (camelCase JSON is the ASP.NET Core default) ----
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// ---- Persistence: EF Core + PostgreSQL ----
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Host=postgres;Port=5432;Database=creditcards;Username=postgres;Password=postgres";

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString, npgsql => npgsql.EnableRetryOnFailure()));

// ---- Messaging: Kafka ----
builder.Services.Configure<KafkaSettings>(builder.Configuration.GetSection(KafkaSettings.SectionName));
builder.Services.AddSingleton<ITransactionEventPublisher, KafkaTransactionEventPublisher>();
builder.Services.AddHostedService<KafkaTopicInitializer>();

// ---- Repositories (only layer that touches the DbContext) ----
builder.Services.AddScoped<ICreditCardRepository, CreditCardRepository>();
builder.Services.AddScoped<ITransactionRepository, TransactionRepository>();

// ---- Use cases (application layer, one class per operation) ----
builder.Services.AddScoped<GetAllCreditCardsUseCase>();
builder.Services.AddScoped<GetCreditCardByIdUseCase>();
builder.Services.AddScoped<CreateCreditCardUseCase>();
builder.Services.AddScoped<UpdateCreditCardUseCase>();
builder.Services.AddScoped<DeleteCreditCardUseCase>();
builder.Services.AddScoped<GetCreditCardTransactionsUseCase>();

builder.Services.AddScoped<GetAllTransactionsUseCase>();
builder.Services.AddScoped<GetTransactionByIdUseCase>();
builder.Services.AddScoped<CreateTransactionUseCase>();
builder.Services.AddScoped<UpdateTransactionUseCase>();
builder.Services.AddScoped<DeleteTransactionUseCase>();

var app = builder.Build();

// Create the schema automatically on startup (with retry while Postgres warms up).
await EnsureDatabaseAsync(app);

app.MapControllers();

app.Run();

static async Task EnsureDatabaseAsync(WebApplication app)
{
    const int maxAttempts = 15;

    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    for (var attempt = 1; attempt <= maxAttempts; attempt++)
    {
        try
        {
            await db.Database.EnsureCreatedAsync();
            logger.LogInformation("Database schema ensured.");
            return;
        }
        catch (Exception ex)
        {
            logger.LogWarning(
                "Database not ready (attempt {Attempt}/{Max}): {Message}",
                attempt, maxAttempts, ex.Message);

            if (attempt == maxAttempts)
            {
                throw;
            }

            await Task.Delay(TimeSpan.FromSeconds(4));
        }
    }
}

// Exposed so integration tests (WebApplicationFactory) can reference the entry point.
public partial class Program { }
