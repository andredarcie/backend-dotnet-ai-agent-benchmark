using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Testcontainers.Kafka;
using Testcontainers.PostgreSql;

namespace CreditCardApi.Tests.Integration;

public sealed class CreditCardApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private const string Key = "MDEyMzQ1Njc4OWFiY2RlZjAxMjM0NTY3ODlhYmNkZWY=";
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:16-alpine")
        .WithDatabase("creditcards_test")
        .WithUsername("creditcards")
        .WithPassword("creditcards_test_password")
        .Build();

    private readonly KafkaContainer _kafka = new KafkaBuilder("confluentinc/cp-kafka:7.6.1")
        .WithKRaft()
        .Build();

    public string KafkaBootstrapServers => _kafka.GetBootstrapAddress();

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
        await _kafka.StartAsync();
        SetEnvironment();
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        ClearEnvironment();
        await base.DisposeAsync();
        await _kafka.DisposeAsync();
        await _postgres.DisposeAsync();
    }


    private void SetEnvironment()
    {
        Environment.SetEnvironmentVariable("ConnectionStrings__DefaultConnection", _postgres.GetConnectionString());
        Environment.SetEnvironmentVariable("Kafka__BootstrapServers", _kafka.GetBootstrapAddress());
        Environment.SetEnvironmentVariable("Kafka__TransactionsTopic", "transactions");
        Environment.SetEnvironmentVariable("Kafka__DeadLetterTopic", "transactions.dlq");
        Environment.SetEnvironmentVariable("Kafka__ConsumerGroupId", $"credit-card-api-tests-{Guid.NewGuid():N}");
        Environment.SetEnvironmentVariable("Kafka__OutboxPollSeconds", "1");
        Environment.SetEnvironmentVariable("Security__PanEncryptionKey", Key);
    }

    private static void ClearEnvironment()
    {
        Environment.SetEnvironmentVariable("ConnectionStrings__DefaultConnection", null);
        Environment.SetEnvironmentVariable("Kafka__BootstrapServers", null);
        Environment.SetEnvironmentVariable("Kafka__TransactionsTopic", null);
        Environment.SetEnvironmentVariable("Kafka__DeadLetterTopic", null);
        Environment.SetEnvironmentVariable("Kafka__ConsumerGroupId", null);
        Environment.SetEnvironmentVariable("Kafka__OutboxPollSeconds", null);
        Environment.SetEnvironmentVariable("Security__PanEncryptionKey", null);
    }
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = _postgres.GetConnectionString(),
                ["Kafka:BootstrapServers"] = _kafka.GetBootstrapAddress(),
                ["Kafka:TransactionsTopic"] = "transactions",
                ["Kafka:DeadLetterTopic"] = "transactions.dlq",
                ["Kafka:ConsumerGroupId"] = $"credit-card-api-tests-{Guid.NewGuid():N}",
                ["Kafka:OutboxPollSeconds"] = "1",
                ["Security:PanEncryptionKey"] = Key
            });
        });
    }
}


