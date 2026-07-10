using CreditCardApi.Api.Infrastructure.Data;
using CreditCardApi.Api.Infrastructure.Messaging;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Serilog.Context;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
    ?? throw new InvalidOperationException("Connection string not found in configuration or environment variable 'ConnectionStrings__DefaultConnection'");

builder.Services.AddDbContext<CreditCardDbContext>(options =>
{
    options.UseNpgsql(connectionString, npgsqlOptions =>
    {
        npgsqlOptions.EnableRetryOnFailure(maxRetryCount: 3);
    });
});

builder.Services.AddSingleton<ITransactionProducer, TransactionProducer>();

builder.Services.AddHealthChecks()
    .AddNpgSql(connectionString, name: "database");

builder.Host.UseSerilog((context, configuration) =>
{
    configuration
        .MinimumLevel.Information()
        .WriteTo.Console(
            outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss zzz}] [{Level:u3}] {Message:lj}{NewLine}{Exception}")
        .Enrich.FromLogContext()
        .Enrich.WithProperty("ApplicationName", "CreditCardApi");
});

var app = builder.Build();

if (!app.Environment.IsProduction())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.Use(async (context, next) =>
{
    var correlationId = context.Request.Headers.TryGetValue("X-Correlation-ID", out var headerCorrelationId)
        ? headerCorrelationId.ToString()
        : context.TraceIdentifier;

    using (LogContext.PushProperty("CorrelationId", correlationId))
    {
        context.Response.Headers["X-Correlation-ID"] = correlationId;
        await next();
    }
});

app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        var exceptionHandlerPathFeature = context.Features.Get<IExceptionHandlerPathFeature>();
        var exception = exceptionHandlerPathFeature?.Error;

        context.Response.ContentType = "application/problem+json";
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;

        var problem = new ProblemDetails
        {
            Status = StatusCodes.Status500InternalServerError,
            Title = "Internal Server Error",
            Detail = "An unexpected error occurred",
            Type = "https://httpwg.org/specs/rfc9110.html#status.500",
            Instance = context.Request.Path
        };

        await context.Response.WriteAsJsonAsync(problem);

        using (LogContext.PushProperty("Exception", exception?.ToString()))
        {
            app.Logger.LogError(exception, "An unhandled exception occurred");
        }
    });
});

app.MapControllers();

app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        var response = new
        {
            status = report.Status.ToString(),
            timestamp = DateTime.UtcNow,
            checks = report.Entries.ToDictionary(x => x.Key, x => new { status = x.Value.Status.ToString() })
        };
        await context.Response.WriteAsJsonAsync(response);
    }
});

app.MapGet("/metrics", () =>
{
    return Results.Ok(new
    {
        timestamp = DateTime.UtcNow,
        version = "1.0",
        environment = app.Environment.EnvironmentName
    });
}).Produces(200).WithName("GetMetrics");

using (var scope = app.Services.CreateScope())
{
    try
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<CreditCardDbContext>();
        await dbContext.Database.MigrateAsync();
        app.Logger.LogInformation("Database migration completed successfully");
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "Database migration failed. Application cannot start.");
        throw;
    }
}

var producer = app.Services.GetService<ITransactionProducer>();
app.Lifetime.ApplicationStopping.Register(async () =>
{
    if (producer is IAsyncDisposable asyncDisposable)
    {
        await asyncDisposable.DisposeAsync();
        app.Logger.LogInformation("Kafka producer disposed gracefully");
    }
});

app.Run();
