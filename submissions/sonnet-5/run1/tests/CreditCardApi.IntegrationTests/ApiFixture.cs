using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Testcontainers.Kafka;
using Testcontainers.PostgreSql;

namespace CreditCardApi.IntegrationTests;

/// <summary>
/// Boots the full app (via <c>WebApplicationFactory</c>) against real Postgres and Kafka containers —
/// the same black-box shape the app runs in under <c>docker compose</c>.
/// </summary>
public class ApiFixture : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:17-alpine")
        .WithDatabase("creditcards_test")
        .WithUsername("test")
        .WithPassword("test")
        .Build();

    private readonly KafkaContainer _kafka = new KafkaBuilder("apache/kafka:4.0.0").Build();

    public string PostgresConnectionString => _postgres.GetConnectionString();

    public string KafkaBootstrapAddress => _kafka.GetBootstrapAddress();

    public async Task InitializeAsync()
    {
        await Task.WhenAll(_postgres.StartAsync(), _kafka.StartAsync());

        // Force the host to start now (migrations + topic creation) rather than lazily on the first test.
        _ = CreateClient();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, configBuilder) =>
        {
            configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"] = _postgres.GetConnectionString(),
                ["Kafka:BootstrapServers"] = _kafka.GetBootstrapAddress(),
            });
        });
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await _postgres.DisposeAsync();
        await _kafka.DisposeAsync();
        await base.DisposeAsync();
    }
}
