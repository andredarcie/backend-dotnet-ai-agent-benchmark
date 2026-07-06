using System.Reflection;
using System.Threading.RateLimiting;
using Asp.Versioning;
using Confluent.Kafka;
using CreditCardApi.Application.Abstractions;
using CreditCardApi.Application.Services;
using CreditCardApi.Data;
using CreditCardApi.Infrastructure;
using CreditCardApi.Infrastructure.Configuration;
using CreditCardApi.Infrastructure.Health;
using CreditCardApi.Infrastructure.Messaging;
using CreditCardApi.Infrastructure.Observability;
using CreditCardApi.Infrastructure.Security;
using CreditCardApi.Presentation;
using CreditCardApi.Presentation.Contracts;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using Serilog.Formatting.Compact;
using Swashbuckle.AspNetCore.SwaggerGen;

Log.Logger = new LoggerConfiguration()
    .Enrich.FromLogContext()
    .WriteTo.Console(new CompactJsonFormatter())
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);
    builder.Configuration.AddEnvironmentVariables();

    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .WriteTo.Console(new CompactJsonFormatter()));

    builder.Services.Configure<HostOptions>(options => options.ShutdownTimeout = TimeSpan.FromSeconds(30));
    builder.Services.AddProblemDetails(options =>
    {
        options.CustomizeProblemDetails = context =>
        {
            context.ProblemDetails.Extensions["traceId"] = context.HttpContext.TraceIdentifier;
            if (context.HttpContext.Response.Headers.TryGetValue(CorrelationIdMiddleware.HeaderName, out var correlationId))
            {
                context.ProblemDetails.Extensions["correlationId"] = correlationId.ToString();
            }
        };
    });
    builder.Services.AddExceptionHandler<ProblemDetailsExceptionHandler>();

    builder.Services.AddControllers()
        .AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        });
    builder.Services.Configure<ApiBehaviorOptions>(options => options.InvalidModelStateResponseFactory = InvalidModelStateResponseFactory.Create);

    builder.Services.AddApiVersioning(options =>
    {
        options.DefaultApiVersion = new ApiVersion(1, 0);
        options.AssumeDefaultVersionWhenUnspecified = true;
        options.ReportApiVersions = true;
        options.ApiVersionReader = ApiVersionReader.Combine(
            new QueryStringApiVersionReader("api-version"),
            new HeaderApiVersionReader("X-API-Version"));
    })
    .AddMvc()
    .AddApiExplorer(options =>
    {
        options.GroupNameFormat = "'v'VVV";
        options.SubstituteApiVersionInUrl = false;
    });

    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(ConfigureSwagger);

    builder.Services.AddOptions<SecurityOptions>()
        .Bind(builder.Configuration.GetSection("Security"))
        .ValidateDataAnnotations()
        .Validate(SecurityOptions.HasValidKey, "Security:PanEncryptionKey must be a base64-encoded 256-bit key.")
        .ValidateOnStart();
    builder.Services.AddOptions<KafkaOptions>()
        .Bind(builder.Configuration.GetSection("Kafka"))
        .ValidateDataAnnotations()
        .ValidateOnStart();

    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    if (string.IsNullOrWhiteSpace(connectionString))
    {
        throw new InvalidOperationException("ConnectionStrings__DefaultConnection must be configured.");
    }

    builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseNpgsql(
        connectionString,
        npgsql => npgsql.EnableRetryOnFailure(5, TimeSpan.FromSeconds(10), null)));

    builder.Services.AddSingleton<IClock, SystemClock>();
    builder.Services.AddSingleton<ICardNumberProtector, AesCardNumberProtector>();
    builder.Services.AddScoped<CreditCardService>();
    builder.Services.AddScoped<TransactionService>();
    builder.Services.AddSingleton(KafkaProducerFactory.Create);
    builder.Services.AddHostedService<DatabaseMigrationService>();
    builder.Services.AddHostedService<KafkaTopicInitializer>();
    builder.Services.AddHostedService<OutboxPublisherService>();
    builder.Services.AddHostedService<TransactionConsumerService>();

    builder.Services.AddRateLimiter(options =>
    {
        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
        options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
            RateLimitPartition.GetFixedWindowLimiter(
                context.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 120,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0,
                    AutoReplenishment = true
                }));
    });

    builder.Services.AddHealthChecks()
        .AddCheck("self", () => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy(), tags: ["live"])
        .AddDbContextCheck<ApplicationDbContext>("postgresql", tags: ["ready"])
        .AddCheck<KafkaHealthCheck>("kafka", tags: ["ready"]);

    builder.Services.AddOpenTelemetry()
        .ConfigureResource(resource => resource.AddService("CreditCardApi"))
        .WithTracing(tracing => tracing
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddNpgsql())
        .WithMetrics(metrics => metrics
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddRuntimeInstrumentation()
            .AddNpgsqlInstrumentation()
            .AddPrometheusExporter());

    builder.Services.AddHsts(options =>
    {
        options.MaxAge = TimeSpan.FromDays(365);
        options.IncludeSubDomains = true;
    });

    var app = builder.Build();

    if (!app.Environment.IsDevelopment())
    {
        app.UseHsts();
    }

    app.UseExceptionHandler();
    app.UseMiddleware<CorrelationIdMiddleware>();
    app.UseSerilogRequestLogging(options =>
    {
        options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
        {
            diagnosticContext.Set("CorrelationId", httpContext.TraceIdentifier);
        };
    });
    app.Use(async (context, next) =>
    {
        context.Response.Headers["X-Content-Type-Options"] = "nosniff";
        context.Response.Headers["X-Frame-Options"] = "DENY";
        context.Response.Headers["Referrer-Policy"] = "no-referrer";
        await next(context);
    });
    app.UseRateLimiter();
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Credit Card API v1");
    });
    app.MapControllers();
    app.MapGet("/health", () => Results.Json(new HealthResponse("healthy")))
        .WithName("Health")
        .Produces<HealthResponse>(StatusCodes.Status200OK);
    app.MapHealthChecks("/health/live", HealthCheckResponseWriter.ForTags("live"));
    app.MapHealthChecks("/health/ready", HealthCheckResponseWriter.ForTags("ready"));
    app.MapPrometheusScrapingEndpoint("/metrics");

    await app.RunAsync();
}
catch (Exception exception) when (exception is not OperationCanceledException)
{
    Log.Fatal(exception, "Application terminated unexpectedly");
    throw;
}
finally
{
    await Log.CloseAndFlushAsync();
}

static void ConfigureSwagger(SwaggerGenOptions options)
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Credit Card API",
        Version = "v1",
        Description = "REST API for credit cards and transactions backed by PostgreSQL and Kafka."
    });
    options.SupportNonNullableReferenceTypes();

    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath, includeControllerXmlComments: true);
    }
}

public partial class Program;


