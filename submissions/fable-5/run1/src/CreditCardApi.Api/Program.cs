using System.Globalization;
using System.Threading.RateLimiting;
using Asp.Versioning;
using CreditCardApi.Api;
using CreditCardApi.Api.ErrorHandling;
using CreditCardApi.Api.HealthChecks;
using CreditCardApi.Api.Middleware;
using CreditCardApi.Application.CreditCards;
using CreditCardApi.Application.Transactions;
using CreditCardApi.Infrastructure;
using CreditCardApi.Infrastructure.Messaging;
using CreditCardApi.Infrastructure.Observability;
using CreditCardApi.Infrastructure.Persistence;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Npgsql;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);

// ---------------------------------------------------------------------------------------------
// Logging: structured JSON to stdout, correlation id included via scopes.
// ---------------------------------------------------------------------------------------------
builder.Logging.ClearProviders();
builder.Logging.AddJsonConsole(options =>
{
    options.IncludeScopes = true;
    options.UseUtcTimestamp = true;
    options.TimestampFormat = "yyyy-MM-dd'T'HH:mm:ss.fff'Z' ";
});

// ---------------------------------------------------------------------------------------------
// OpenTelemetry: traces and metrics with a Prometheus endpoint; OTLP export activates only when
// an OTEL_EXPORTER_OTLP_ENDPOINT is configured, so the default compose setup stays quiet.
// ---------------------------------------------------------------------------------------------
var otlpConfigured = !string.IsNullOrEmpty(builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]);

builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService(
        serviceName: "credit-card-api",
        serviceVersion: typeof(Program).Assembly.GetName().Version?.ToString(3)))
    .WithTracing(tracing =>
    {
        tracing
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddNpgsql();
        if (otlpConfigured)
        {
            tracing.AddOtlpExporter();
        }
    })
    .WithMetrics(metrics =>
    {
        metrics
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddRuntimeInstrumentation()
            .AddPrometheusExporter();
        if (otlpConfigured)
        {
            metrics.AddOtlpExporter();
        }
    });

// ---------------------------------------------------------------------------------------------
// MVC, problem details, validation and versioning.
// ---------------------------------------------------------------------------------------------
builder.Services.AddControllers();

builder.Services.AddProblemDetails(options => options.CustomizeProblemDetails = context =>
{
    if (context.HttpContext.Items.TryGetValue(Correlation.HeaderName, out var correlationId))
    {
        context.ProblemDetails.Extensions["correlationId"] = correlationId;
    }
});
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

builder.Services
    .AddApiVersioning(options =>
    {
        options.DefaultApiVersion = new ApiVersion(1, 0);
        options.AssumeDefaultVersionWhenUnspecified = true;
        options.ReportApiVersions = true;
        // Versioning rides on a query parameter or header so the mandated routes stay stable.
        options.ApiVersionReader = ApiVersionReader.Combine(
            new QueryStringApiVersionReader("api-version"),
            new HeaderApiVersionReader("X-Api-Version"));
    })
    .AddMvc()
    .AddApiExplorer();

builder.Services.AddOpenApi(options => options.AddDocumentTransformer((document, _, _) =>
{
    document.Info.Title = "Credit Card API";
    document.Info.Version = "v1";
    document.Info.Description =
        "REST API for managing credit cards and their transactions. Created transactions are " +
        "published to the `transactions` Kafka topic. Card numbers are stored truncated and " +
        "returned masked.";
    return Task.CompletedTask;
}));

// ---------------------------------------------------------------------------------------------
// Rate limiting: fixed window per client IP, applied to the API endpoints only.
// ---------------------------------------------------------------------------------------------
builder.Services.AddOptions<RateLimitingOptions>()
    .Bind(builder.Configuration.GetSection(RateLimitingOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

var rateLimiting = builder.Configuration
    .GetSection(RateLimitingOptions.SectionName)
    .Get<RateLimitingOptions>() ?? new RateLimitingOptions();

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = (context, _) =>
    {
        context.HttpContext.Response.Headers.RetryAfter =
            rateLimiting.WindowSeconds.ToString(CultureInfo.InvariantCulture);
        return ValueTask.CompletedTask;
    };
    options.AddPolicy("api", httpContext => RateLimitPartition.GetFixedWindowLimiter(
        httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = rateLimiting.PermitLimit,
            Window = TimeSpan.FromSeconds(rateLimiting.WindowSeconds),
            QueueLimit = 0,
        }));
});

// ---------------------------------------------------------------------------------------------
// Application and infrastructure services.
// ---------------------------------------------------------------------------------------------
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<CreditCardService>();
builder.Services.AddScoped<TransactionService>();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddHealthChecks()
    .AddDbContextCheck<CreditCardDbContext>("postgres", tags: ["ready"])
    .AddCheck<KafkaHealthCheck>("kafka", tags: ["ready"]);

var app = builder.Build();

// ---------------------------------------------------------------------------------------------
// Pipeline. TLS terminates at the ingress/load balancer in production; the container itself
// serves plain HTTP on 8080, so there is deliberately no HTTPS redirect here. HSTS is emitted
// for any HTTPS traffic outside development.
// ---------------------------------------------------------------------------------------------
if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

app.UseExceptionHandler();
app.UseStatusCodePages(); // gives bare 4xx responses (e.g. NotFound()) an RFC 9457 body
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseRateLimiter();

app.MapControllers().RequireRateLimiting("api");

app.MapOpenApi(); // serves /openapi/v1.json
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/openapi/v1.json", "Credit Card API v1");
    options.RoutePrefix = "swagger";
});

app.MapPrometheusScrapingEndpoint("/metrics");

// Liveness: the process is up and serving. No dependency checks — a broker outage must not get
// the container restarted.
var liveness = new HealthCheckOptions
{
    Predicate = _ => false,
    ResponseWriter = HealthResponseWriter.WriteMinimalAsync,
};
app.MapHealthChecks("/health", liveness);
app.MapHealthChecks("/health/live", liveness);

// Readiness: the service can actually do work (database and broker reachable).
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = registration => registration.Tags.Contains("ready"),
    ResponseWriter = HealthResponseWriter.WriteDetailedAsync,
});

// Bring dependencies to a known state before serving traffic: schema migrated, topics created.
await DatabaseInitializer.MigrateAsync(app.Services, CancellationToken.None);
await KafkaTopicInitializer.EnsureTopicsAsync(app.Services, CancellationToken.None);

await app.RunAsync();

/// <summary>Entry point marker used by <c>WebApplicationFactory</c> in the integration tests.</summary>
public partial class Program
{
}
