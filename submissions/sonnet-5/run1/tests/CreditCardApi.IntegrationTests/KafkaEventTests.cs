using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Confluent.Kafka;
using CreditCardApi.Application.CreditCards;
using CreditCardApi.Application.Transactions;
using Npgsql;

namespace CreditCardApi.IntegrationTests;

[Collection(ApiCollectionDefinition.Name)]
public class KafkaEventTests(ApiFixture fixture)
{
    [Fact]
    public async Task CreatingATransaction_PublishesItToTheTransactionsTopic()
    {
        using var client = fixture.CreateClient();
        var card = await CreateCardAsync(client);

        var httpResponse = await client.PostAsJsonAsync(
            "/api/transactions",
            new TransactionRequest { CreditCardId = card.Id, Amount = 42.50m, Merchant = "Kafka Test Store" },
            Contracts.Json);
        var created = (await httpResponse.Content.ReadFromJsonAsync<TransactionResponse>(Contracts.Json))!;
        var correlationId = httpResponse.Headers.GetValues("X-Correlation-Id").Single();

        using var consumer = BuildConsumer($"kafka-event-test-{Guid.NewGuid():n}");
        consumer.Subscribe("transactions");

        var message = ConsumeUntil(consumer, result => result.Message.Key == created.Id.ToString(), TimeSpan.FromSeconds(30));

        Assert.NotNull(message);
        var payload = JsonSerializer.Deserialize<TransactionResponse>(message!.Message.Value, Contracts.Json)!;
        Assert.Equal(created.Id, payload.Id);
        Assert.Equal(created.Amount, payload.Amount);
        Assert.Equal("Kafka Test Store", payload.Merchant);
        var correlationHeader = message.Message.Headers.FirstOrDefault(h => h.Key == "x-correlation-id");
        Assert.NotNull(correlationHeader);
        Assert.Equal(correlationId, Encoding.UTF8.GetString(correlationHeader!.GetValueBytes()));
    }

    [Fact]
    public async Task CreatingATransaction_IsRecordedByTheIdempotentConsumer()
    {
        using var client = fixture.CreateClient();
        var card = await CreateCardAsync(client);

        var httpResponse = await client.PostAsJsonAsync(
            "/api/transactions",
            new TransactionRequest { CreditCardId = card.Id, Amount = 12m, Merchant = "Idempotency Test" },
            Contracts.Json);
        var created = (await httpResponse.Content.ReadFromJsonAsync<TransactionResponse>(Contracts.Json))!;

        var consumedCount = await PollUntilAsync(
            () => CountConsumedEventsAsync(created.Id), count => count >= 1, TimeSpan.FromSeconds(30));

        Assert.Equal(1, consumedCount);
    }

    [Fact]
    public async Task APoisonMessage_IsRoutedToTheDeadLetterTopic()
    {
        using var producer = new ProducerBuilder<string, string>(
            new ProducerConfig { BootstrapServers = fixture.KafkaBootstrapAddress }).Build();
        var poisonKey = $"poison-{Guid.NewGuid():n}";

        await producer.ProduceAsync("transactions", new Message<string, string> { Key = poisonKey, Value = "{ not-valid-json" });
        producer.Flush(TimeSpan.FromSeconds(5));

        using var consumer = BuildConsumer($"dlq-test-{Guid.NewGuid():n}");
        consumer.Subscribe("transactions.dlq");

        var message = ConsumeUntil(consumer, result => result.Message.Key == poisonKey, TimeSpan.FromSeconds(30));

        Assert.NotNull(message);
        var reasonHeader = message!.Message.Headers.FirstOrDefault(h => h.Key == "x-dead-letter-reason");
        Assert.NotNull(reasonHeader);
        Assert.Equal("deserialize-failure", Encoding.UTF8.GetString(reasonHeader!.GetValueBytes()));
    }

    private IConsumer<string, string> BuildConsumer(string groupId)
    {
        var config = new ConsumerConfig
        {
            BootstrapServers = fixture.KafkaBootstrapAddress,
            GroupId = groupId,
            AutoOffsetReset = AutoOffsetReset.Earliest,
        };
        return new ConsumerBuilder<string, string>(config).Build();
    }

    private static ConsumeResult<string, string>? ConsumeUntil(
        IConsumer<string, string> consumer, Func<ConsumeResult<string, string>, bool> predicate, TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < deadline)
        {
            var result = consumer.Consume(TimeSpan.FromSeconds(1));
            if (result?.Message is not null && predicate(result))
            {
                return result;
            }
        }

        return null;
    }

    private static async Task<T> PollUntilAsync<T>(Func<Task<T>> poll, Func<T, bool> isDone, TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow + timeout;
        var last = await poll();
        while (!isDone(last) && DateTime.UtcNow < deadline)
        {
            await Task.Delay(TimeSpan.FromMilliseconds(500));
            last = await poll();
        }

        return last;
    }

    private async Task<int> CountConsumedEventsAsync(int transactionId)
    {
        await using var connection = new NpgsqlConnection(fixture.PostgresConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(
            "SELECT COUNT(*) FROM consumed_transaction_events WHERE transaction_id = @id", connection);
        command.Parameters.AddWithValue("id", transactionId);
        return Convert.ToInt32(await command.ExecuteScalarAsync());
    }

    private static async Task<CreditCardResponse> CreateCardAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync(
            "/api/credit-cards",
            new CreditCardRequest { CardholderName = Guid.NewGuid().ToString(), CardNumber = "4111111111111111", CreditLimit = 5000m },
            Contracts.Json);
        return (await response.Content.ReadFromJsonAsync<CreditCardResponse>(Contracts.Json))!;
    }
}
