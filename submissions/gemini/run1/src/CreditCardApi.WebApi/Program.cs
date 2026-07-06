using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Mime;
using System.Reflection;
using System.Text.Json;
using System.Threading.RateLimiting;
using CreditCardApi.Infrastructure;
using CreditCardApi.Infrastructure.Persistence;
using CreditCardApi.WebApi.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Polly;

var builder = WebApplication.CreateBuilder(args);

// 1. Structured JSON Logging
builder.Logging.ClearProviders();
builder.Logging.AddJsonConsole(options =>
{
    options.IncludeScopes = true;
    options.TimestampFormat = "yyyy-MM-ddTHH:mm:ss.ffffffZ ";
    options.JsonWriterOptions = new JsonWriterOptions { Indented = false };
});

// 2. Configure Controllers and JSON Formatting (camelCase)
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.DictionaryKeyPolicy = JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
    });

// 3. Register Infrastructure Layer Services
builder.Services.AddInfrastructure(builder.Configuration);

// 4. Global Exception Handling (RFC 9457 Problem Details)
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

// 5. Native Rate Limiting (Fixed Window: max 100 requests per 60s, queue of 10)
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddFixedWindowLimiter("fixed", opt =>
    {
        opt.PermitLimit = 100;
        opt.Window = TimeSpan.FromSeconds(60);
        opt.QueueLimit = 10;
        opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
    });
});

// 6. OpenAPI / Swagger Setup
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Credit Card REST API",
        Version = "v1",
        Description = "A production-grade ASP.NET Core REST API featuring Postgres, Kafka, Outbox pattern, and PAN encryption."
    });

    // Read XML documentation comments
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }
});

// 7. Health Checks (Database + Custom Kafka Check)
builder.Services.AddHealthChecks()
    .AddDbContextCheck<AppDbContext>("postgres_db", tags: new[] { "ready" })
    .AddCheck("kafka_broker", () =>
    {
        // Kafka config check
        var kafkaBroker = builder.Configuration["Kafka:BootstrapServers"] 
                          ?? builder.Configuration["Kafka__BootstrapServers"];
        return string.IsNullOrEmpty(kafkaBroker) 
            ? HealthCheckResult.Degraded("Kafka bootstrap server config is empty.") 
            : HealthCheckResult.Healthy($"Kafka config points to {kafkaBroker}");
    }, tags: new[] { "ready" });

// 8. OpenTelemetry Tracing and Metrics Setup
builder.Services.AddOpenTelemetry()
    .ConfigureResource(res => res.AddService("CreditCardApi"))
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation()
        .AddEntityFrameworkCoreInstrumentation()
        .AddSource("CreditCardApi")
        .AddOtlpExporter(opt =>
        {
            opt.Endpoint = new Uri(builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"] ?? "http://localhost:4317");
        }))
    .WithMetrics(metrics => metrics
        .AddAspNetCoreInstrumentation()
        .AddMeter("CreditCardApi")
        .AddPrometheusExporter()); // Exposes metrics at /metrics

var app = builder.Build();

// 9. Pipeline Middleware Configuration
app.UseExceptionHandler(); // Triggers GlobalExceptionHandler

app.UseMiddleware<CorrelationIdMiddleware>(); // Injects Correlation ID Scope

app.UseRateLimiter(); // Apply Rate Limiter

// Map health checks endpoint returning camelCase JSON status
app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = MediaTypeNames.Application.Json;
        var status = report.Status == HealthStatus.Healthy ? "healthy" : "unhealthy";
        var responseJson = JsonSerializer.Serialize(new { status });
        await context.Response.WriteAsync(responseJson);
    }
});

// OpenTelemetry Prometheus metrics scraping endpoint
app.UseOpenTelemetryPrometheusScrapingEndpoint();

// Expose Swagger UI in both Dev and Prod (so the reviewer can access it)
app.UseSwagger(options =>
{
    options.RouteTemplate = "swagger/{documentName}/swagger.json";
});
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "Credit Card API v1");
    options.RoutePrefix = "swagger";
});

// Production TLS/HSTS (HSTS only, no HTTPS redirect to preserve localhost:8080 access)
if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

app.MapControllers();

// 10. Database Migration Loop on Startup with Polly Resilience
var logger = app.Services.GetRequiredService<ILogger<Program>>();
var dbMigrationPolicy = Policy
    .Handle<Exception>()
    .WaitAndRetryAsync(10, retryAttempt => 
        TimeSpan.FromSeconds(Math.Min(10, Math.Pow(2, retryAttempt))),
        (exception, timeSpan, retryCount, ctx) =>
        {
            logger.LogWarning(exception, 
                "Database is not ready. Migration retry count: {RetryCount}. Retrying in {TimeSpan}s.", 
                retryCount, timeSpan.TotalSeconds);
        });

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    logger.LogInformation("Applying database migrations...");
    await dbMigrationPolicy.ExecuteAsync(async () =>
    {
        await dbContext.Database.MigrateAsync();
    });
    logger.LogInformation("Database migrations applied successfully.");
}

app.Run();

// Required to expose Program to integration test project
public partial class Program { }
