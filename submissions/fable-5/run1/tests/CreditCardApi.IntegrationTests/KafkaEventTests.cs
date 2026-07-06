using System.Globalization;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Confluent.Kafka;

namespace CreditCardApi.IntegrationTests;

[Collection(ApiCollectionDefinition.Name)]
public class KafkaEventTests
{
    private static readonly TimeSpan EventTimeout = TimeSpan.FromSeconds(60);

    private readonly ApiFixture _fixture;

    public KafkaEventTests(ApiFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task CreatedTransaction_IsPublishedToKafkaKeyedByItsId()
    {
        var card = await CreateCardAsync();
        var response = await _fixture.Client.PostAsJsonAsync("/api/transactions", new
        {
            creditCardId = card.Id,
            amount = 199.90,
            merchant = "Amazon",
            category = "shopping",
        });
        response.EnsureSuccessStatusCode();
        var created = (await response.Content.ReadFromJsonAsync<TransactionDto>())!;

        var expectedKey = created.Id.ToString(CultureInfo.InvariantCulture);
        var message = ConsumeUntil("transactions", result => result.Message.Key == expectedKey);

        Assert.NotNull(message);
        using var document = JsonDocument.Parse(message.Message.Value);
        var root = document.RootElement;
        Assert.Equal(created.Id, root.GetProperty("id").GetInt32());
        Assert.Equal(card.Id, root.GetProperty("creditCardId").GetInt32());
        Assert.Equal(199.90m, root.GetProperty("amount").GetDecimal());
        Assert.Equal("Amazon", root.GetProperty("merchant").GetString());
        Assert.Equal("shopping", root.GetProperty("category").GetString());
        Assert.Equal(created.CreatedAt, root.GetProperty("createdAt").GetDateTime());
    }

    [Fact]
    public async Task FailedTransactionCreate_PublishesNothing()
    {
        var response = await _fixture.Client.PostAsJsonAsync("/api/transactions", new
        {
            creditCardId = 987654, // does not exist -> 400, so no event may appear
            amount = 55.55,
            merchant = "Nowhere",
        });

        Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);

        var message = ConsumeUntil(
            "transactions",
            result => result.Message.Value.Contains("987654", StringComparison.Ordinal),
            TimeSpan.FromSeconds(5));

        Assert.Null(message);
    }

    [Fact]
    public void MalformedMessage_IsRoutedToTheDeadLetterTopic()
    {
        var marker = Guid.NewGuid().ToString("N");
        using (var producer = new ProducerBuilder<string, string>(new ProducerConfig
        {
            BootstrapServers = _fixture.KafkaBootstrapServers,
        }).Build())
        {
            producer.Produce("transactions", new Message<string, string>
            {
                Key = marker,
                Value = $"this-is-not-json-{marker}",
            });
            producer.Flush(TimeSpan.FromSeconds(10));
        }

        var deadLettered = ConsumeUntil("transactions.dlq", result => result.Message.Key == marker);

        Assert.NotNull(deadLettered);
        Assert.Contains(marker, deadLettered.Message.Value, StringComparison.Ordinal);
        var reason = deadLettered.Message.Headers.FirstOrDefault(h => h.Key == "x-dead-letter-reason");
        Assert.NotNull(reason);
        Assert.Contains("Malformed", Encoding.UTF8.GetString(reason.GetValueBytes()), StringComparison.Ordinal);
    }

    private ConsumeResult<string, string>? ConsumeUntil(
        string topic,
        Func<ConsumeResult<string, string>, bool> predicate,
        TimeSpan? timeout = null)
    {
        var config = new ConsumerConfig
        {
            BootstrapServers = _fixture.KafkaBootstrapServers,
            GroupId = $"test-{Guid.NewGuid():N}",
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false,
        };

        using var consumer = new ConsumerBuilder<string, string>(config).Build();
        consumer.Subscribe(topic);

        var deadline = DateTime.UtcNow + (timeout ?? EventTimeout);
        try
        {
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
        finally
        {
            consumer.Close();
        }
    }

    private async Task<CardDto> CreateCardAsync()
    {
        var response = await _fixture.Client.PostAsJsonAsync("/api/credit-cards", new
        {
            cardholderName = "Ada Lovelace",
            cardNumber = "4111111111111111",
            brand = "VISA",
            creditLimit = 5000.00,
        });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<CardDto>())!;
    }
}
