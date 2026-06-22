using CreditCardApi.Data;
using CreditCardApi.Kafka;
using CreditCardApi.Repositories;
using CreditCardApi.Repositories.Interfaces;
using CreditCardApi.UseCases.CreditCards;
using CreditCardApi.UseCases.Transactions;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<ICreditCardRepository, CreditCardRepository>();
builder.Services.AddScoped<ITransactionRepository, TransactionRepository>();

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

builder.Services.AddSingleton<IKafkaProducer, KafkaProducer>();

var app = builder.Build();

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
        catch (Exception ex) when (attempt < 10)
        {
            logger.LogWarning("Migration attempt {Attempt} failed: {Message}. Retrying in 3s...", attempt, ex.Message);
            await Task.Delay(3000);
        }
    }
}

app.MapControllers();
app.Run();
