using CreditCardApi.Data;
using CreditCardApi.Repositories;
using CreditCardApi.Services;
using CreditCardApi.UseCases;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Server=postgres;Port=5432;Database=creditcard_db;User Id=postgres;Password=postgres;";

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services.AddScoped<ICreditCardRepository, CreditCardRepository>();
builder.Services.AddScoped<ITransactionRepository, TransactionRepository>();
builder.Services.AddScoped<IKafkaProducerService, KafkaProducerService>();

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

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    dbContext.Database.EnsureCreated();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run("http://0.0.0.0:8080");
