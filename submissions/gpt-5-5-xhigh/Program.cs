using CreditCardApi.Application.UseCases.CreditCards;
using CreditCardApi.Application.UseCases.Transactions;
using CreditCardApi.Application.Messaging;
using CreditCardApi.Infrastructure.Messaging;
using CreditCardApi.Infrastructure.Persistence;
using CreditCardApi.Infrastructure.Repositories;
using CreditCardApi.Infrastructure.Startup;
using CreditCardApi.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.UseUrls("http://0.0.0.0:8080");

builder.Services.AddControllers();
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<ICreditCardRepository, CreditCardRepository>();
builder.Services.AddScoped<ITransactionRepository, TransactionRepository>();
builder.Services.AddScoped<IDatabaseInitializer, DatabaseInitializer>();

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

builder.Services.Configure<KafkaOptions>(builder.Configuration.GetSection(KafkaOptions.SectionName));
builder.Services.AddSingleton<ITransactionEventPublisher, KafkaTransactionEventPublisher>();

var app = builder.Build();

await InitializeDatabaseAsync(app.Services, app.Logger);

app.MapControllers();
app.Run();

static async Task InitializeDatabaseAsync(IServiceProvider services, ILogger logger)
{
    const int maximumAttempts = 30;

    for (var attempt = 1; attempt <= maximumAttempts; attempt++)
    {
        try
        {
            await using var scope = services.CreateAsyncScope();
            var initializer = scope.ServiceProvider.GetRequiredService<IDatabaseInitializer>();
            await initializer.InitializeAsync();
            return;
        }
        catch (Exception exception) when (attempt < maximumAttempts)
        {
            logger.LogWarning(exception, "Database initialization attempt {Attempt} failed", attempt);
            await Task.Delay(TimeSpan.FromSeconds(2));
        }
    }
}

public partial class Program;
