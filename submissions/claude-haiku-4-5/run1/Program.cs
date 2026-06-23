using CreditCardApi.Data;
using CreditCardApi.Data.Repositories;
using CreditCardApi.Infrastructure;
using CreditCardApi.UseCases;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Host=postgres;Port=5432;Database=creditcard_db;Username=creditcard_user;Password=creditcard_password";
builder.Services.AddDbContext<CreditCardDbContext>(options =>
    options.UseNpgsql(connectionString));

var kafkaBootstrapServers = builder.Configuration["Kafka:BootstrapServers"] ?? "kafka:9092";
builder.Services.AddSingleton<IKafkaProducer>(sp =>
    new KafkaProducer(kafkaBootstrapServers, sp.GetRequiredService<ILogger<KafkaProducer>>()));

builder.Services.AddScoped<ICreditCardRepository, CreditCardRepository>();
builder.Services.AddScoped<ITransactionRepository, TransactionRepository>();

builder.Services.AddScoped<CreateCreditCardUseCase>();
builder.Services.AddScoped<GetCreditCardByIdUseCase>();
builder.Services.AddScoped<GetAllCreditCardsUseCase>();
builder.Services.AddScoped<UpdateCreditCardUseCase>();
builder.Services.AddScoped<DeleteCreditCardUseCase>();
builder.Services.AddScoped<GetCreditCardTransactionsUseCase>();

builder.Services.AddScoped<CreateTransactionUseCase>();
builder.Services.AddScoped<GetTransactionByIdUseCase>();
builder.Services.AddScoped<GetAllTransactionsUseCase>();
builder.Services.AddScoped<UpdateTransactionUseCase>();
builder.Services.AddScoped<DeleteTransactionUseCase>();

var app = builder.Build();

app.Urls.Clear();
app.Urls.Add("http://0.0.0.0:8080");

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<CreditCardDbContext>();
    await db.Database.EnsureCreatedAsync();
}

app.UseAuthorization();
app.MapControllers();

app.Run();
