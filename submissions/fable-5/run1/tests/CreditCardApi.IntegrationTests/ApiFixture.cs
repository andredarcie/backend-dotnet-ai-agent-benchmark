using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Testcontainers.Kafka;
using Testcontainers.PostgreSql;

namespace CreditCardApi.IntegrationTests;

/// <summary>
/// Boots the full application once per test collection against throwaway PostgreSQL and Kafka
/// containers, so the tests exercise the real stack end to end (migrations, outbox, consumer).
/// </summary>
public sealed class ApiFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:17-alpine")
        .WithDatabase("creditcards")
        .Build();

    private readonly KafkaContainer _kafka = new KafkaBuilder("confluentinc/cp-kafka:7.6.1")
        .Build();

    private WebApplicationFactory<Program>? _factory;

    /// <summary>Client against the in-memory test server.</summary>
    public HttpClient Client { get; private set; } = null!;

    /// <summary>Bootstrap address of the throwaway Kafka broker, for test consumers/producers.</summary>
    public string KafkaBootstrapServers { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        await Task.WhenAll(_postgres.StartAsync(), _kafka.StartAsync());
        KafkaBootstrapServers = _kafka.GetBootstrapAddress();

        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseSetting("ConnectionStrings:Default", _postgres.GetConnectionString());
            builder.UseSetting("Kafka:BootstrapServers", KafkaBootstrapServers);
            // Poll faster than the production default so event assertions complete quickly.
            builder.UseSetting("Kafka:OutboxPollIntervalMs", "250");
        });

        Client = _factory.CreateClient();
    }

    public async Task DisposeAsync()
    {
        if (_factory is not null)
        {
            await _factory.DisposeAsync();
        }

        await Task.WhenAll(_postgres.DisposeAsync().AsTask(), _kafka.DisposeAsync().AsTask());
    }
}

/// <summary>Single shared application instance across all integration test classes.</summary>
[CollectionDefinition(Name)]
public sealed class ApiCollectionDefinition : ICollectionFixture<ApiFixture>
{
    /// <summary>Collection name.</summary>
    public const string Name = "api";
}
