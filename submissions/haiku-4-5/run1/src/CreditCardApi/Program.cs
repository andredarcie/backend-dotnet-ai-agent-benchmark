using CreditCardApi.Application.Repositories;
using CreditCardApi.Application.Services;
using CreditCardApi.Data;
using CreditCardApi.Data.Repositories;
using CreditCardApi.Infrastructure.Messaging;
using CreditCardApi.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Swashbuckle.AspNetCore.SwaggerGen;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, loggerConfig) =>
{
    loggerConfig
        .ReadFrom.Configuration(context.Configuration)
        .WriteTo.Console(outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}");
});

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddSwaggerGen();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Host=postgres;Port=5432;Database=creditcarddb;Username=postgres;Password=postgres";

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString, npgsqlOptions =>
    {
        npgsqlOptions.EnableRetryOnFailure(3, TimeSpan.FromSeconds(5), null);
    }));

builder.Services.AddScoped<ICreditCardRepository, CreditCardRepository>();
builder.Services.AddScoped<ITransactionRepository, TransactionRepository>();
builder.Services.AddScoped<ITransactionService, TransactionService>();

var kafkaBootstrapServers = builder.Configuration["Kafka:BootstrapServers"] ?? "kafka:9092";
builder.Services.AddSingleton<IKafkaProducer>(sp =>
{
    var logger = sp.GetRequiredService<ILogger<KafkaProducer>>();
    return new KafkaProducer(kafkaBootstrapServers, logger);
});

builder.Services.AddHostedService<OutboxPublisherService>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    app.Logger.LogInformation("Applying database migrations");
    await db.Database.MigrateAsync();
    app.Logger.LogInformation("Database migrations completed");
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Credit Card API v1");
    c.RoutePrefix = "swagger";
});

app.MapControllers();
app.MapHealthChecks("/health");

app.Run();
