using System.Threading.RateLimiting;
using CreditCardApi.Api.HealthChecks;
using CreditCardApi.Api.Middleware;
using CreditCardApi.Application;
using CreditCardApi.Infrastructure;
using CreditCardApi.Infrastructure.Persistence;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.OpenApi;
using Scalar.AspNetCore;
using Serilog;
using Serilog.Formatting.Compact;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .Enrich.FromLogContext()
    .WriteTo.Console(new CompactJsonFormatter())
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .Enrich.FromLogContext()
        .WriteTo.Console(new CompactJsonFormatter()));

    builder.Services.AddSingleton(TimeProvider.System);

    builder.Services.AddControllers();

    builder.Services.AddProblemDetails();
    builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

    builder.Services.AddApplication();
    builder.Services.AddInfrastructure(builder.Configuration);

    builder.Services.AddOpenApi(options =>
    {
        options.AddDocumentTransformer((document, _, _) =>
        {
            document.Info = new OpenApiInfo
            {
                Title = "Credit Card API",
                Version = "v1",
                Description = "REST API for credit cards and their transactions, backed by PostgreSQL and Apache Kafka.",
            };
            return Task.CompletedTask;
        });
    });

    builder.Services.AddRateLimiter(options =>
    {
        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
        options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
            RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                factory: _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 100,
                    Window = TimeSpan.FromSeconds(10),
                    QueueLimit = 0,
                }));
    });

    var app = builder.Build();

    await DatabaseMigrator.MigrateAsync(app.Services, CancellationToken.None);

    // Correlation id first: it pushes onto Serilog's LogContext, so the request-logging
    // middleware's own summary line (and everything downstream) carries it too.
    app.UseMiddleware<CorrelationIdMiddleware>();

    app.UseSerilogRequestLogging();

    app.UseExceptionHandler();

    app.UseRateLimiter();

    app.MapOpenApi();
    app.MapScalarApiReference();

    app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

    app.MapHealthChecks("/health/ready", new HealthCheckOptions
    {
        Predicate = check => check.Tags.Contains("ready"),
        ResponseWriter = HealthCheckResponseWriter.WriteResponse,
    });

    app.MapControllers();

    await app.RunAsync();
}
finally
{
    await Log.CloseAndFlushAsync();
}
