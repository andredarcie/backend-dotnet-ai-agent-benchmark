using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Testcontainers.Kafka;
using Testcontainers.PostgreSql;
using Xunit;

namespace CreditCardApi.IntegrationTests;

/// <summary>
/// Boots the real API against throwaway PostgreSQL and Kafka containers, so the integration tests
/// exercise the full stack (migrations, persistence, outbox and Kafka) end to end.
/// </summary>
public sealed class CreditCardApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase("creditcards")
        .Build();

    private readonly KafkaContainer _kafka = new KafkaBuilder()
        .WithImage("confluentinc/cp-kafka:7.6.1")
        .Build();

    /// <summary>The Kafka bootstrap address for tests that consume directly.</summary>
    public string KafkaBootstrapServers => _kafka.GetBootstrapAddress().Replace("PLAINTEXT://", string.Empty);

    /// <inheritdoc />
    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
        await _kafka.StartAsync();
    }

    /// <inheritdoc />
    public new async Task DisposeAsync()
    {
        await _kafka.DisposeAsync();
        await _postgres.DisposeAsync();
        await base.DisposeAsync();
    }

    /// <inheritdoc />
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.UseSetting("ConnectionStrings:Default", _postgres.GetConnectionString());
        builder.UseSetting("Kafka:BootstrapServers", KafkaBootstrapServers);
        builder.UseSetting("Outbox:PollIntervalSeconds", "1");
    }
}
