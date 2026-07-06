using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Confluent.Kafka;
using CreditCardApi.Application.Dtos;
using FluentAssertions;

namespace CreditCardApi.Tests.Integration;

public sealed class ApiAcceptanceTests : IClassFixture<CreditCardApiFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly CreditCardApiFactory _factory;
    private readonly HttpClient _client;

    public ApiAcceptanceTests(CreditCardApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [DockerFact]
    public async Task Health_returns_healthy_payload()
    {
        var response = await _client.GetFromJsonAsync<JsonElement>("/health");

        response.GetProperty("status").GetString().Should().Be("healthy");
    }

    [DockerFact]
    public async Task Credit_card_create_returns_location_and_masked_card_number()
    {
        var card = await CreateCardAsync();

        card.Id.Should().BeGreaterThan(0);
        card.CardNumber.Should().EndWith("1111");
        card.CardNumber.Should().NotContain("4111111111111111");
    }

    [DockerFact]
    public async Task Credit_card_create_rejects_blank_required_fields_as_problem_details()
    {
        var response = await _client.PostAsJsonAsync("/api/credit-cards", new
        {
            cardholderName = " ",
            cardNumber = " ",
            creditLimit = 100m
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");
    }

    [DockerFact]
    public async Task Transaction_create_rejects_missing_credit_card_fk()
    {
        var response = await _client.PostAsJsonAsync("/api/transactions", new
        {
            creditCardId = 987654,
            amount = 199.90m,
            merchant = "Amazon",
            category = "shopping"
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");
    }

    [DockerFact]
    public async Task Transaction_create_rejects_non_positive_amount()
    {
        var card = await CreateCardAsync();

        var response = await _client.PostAsJsonAsync("/api/transactions", new
        {
            creditCardId = card.Id,
            amount = 0m,
            merchant = "Amazon"
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [DockerFact]
    public async Task Transaction_create_publishes_created_transaction_to_kafka()
    {
        var card = await CreateCardAsync();
        var createResponse = await _client.PostAsJsonAsync("/api/transactions", new
        {
            creditCardId = card.Id,
            amount = 199.90m,
            merchant = "Amazon",
            category = "shopping"
        });
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var transaction = await createResponse.Content.ReadFromJsonAsync<TransactionResponse>();
        transaction.Should().NotBeNull();

        using var consumer = new ConsumerBuilder<string, string>(new ConsumerConfig
        {
            BootstrapServers = _factory.KafkaBootstrapServers,
            GroupId = $"acceptance-{Guid.NewGuid():N}",
            EnableAutoCommit = false,
            AutoOffsetReset = AutoOffsetReset.Earliest
        }).Build();

        consumer.Subscribe("transactions");
        var consumed = WaitForTransactionEvent(consumer, transaction!.Id, TimeSpan.FromSeconds(30));

        consumed.Should().NotBeNull();
        consumed!.Message.Key.Should().Be(transaction.Id.ToString(System.Globalization.CultureInfo.InvariantCulture));
        var payload = JsonSerializer.Deserialize<TransactionResponse>(consumed.Message.Value, JsonOptions);
        payload.Should().NotBeNull();
        payload!.Merchant.Should().Be("Amazon");
        payload.Amount.Should().Be(199.90m);
    }

    [DockerFact]
    public async Task Transaction_create_handles_concurrent_requests_without_server_errors()
    {
        var card = await CreateCardAsync();
        var tasks = Enumerable.Range(1, 24).Select(index => _client.PostAsJsonAsync("/api/transactions", new
        {
            creditCardId = card.Id,
            amount = 10m + index,
            merchant = $"Merchant {index}"
        }));

        var responses = await Task.WhenAll(tasks);

        responses.Should().OnlyContain(response => (int)response.StatusCode < 500);
        responses.Should().OnlyContain(response => response.StatusCode == HttpStatusCode.Created);
    }

    private async Task<CreditCardResponse> CreateCardAsync()
    {
        var response = await _client.PostAsJsonAsync("/api/credit-cards", new
        {
            cardholderName = "Ada Lovelace",
            cardNumber = "4111111111111111",
            brand = "VISA",
            creditLimit = 5000m
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location.Should().NotBeNull();
        var card = await response.Content.ReadFromJsonAsync<CreditCardResponse>();
        return card!;
    }

    private static ConsumeResult<string, string>? WaitForTransactionEvent(IConsumer<string, string> consumer, int transactionId, TimeSpan timeout)
    {
        var deadline = DateTimeOffset.UtcNow.Add(timeout);
        while (DateTimeOffset.UtcNow < deadline)
        {
            var result = consumer.Consume(TimeSpan.FromMilliseconds(500));
            if (result?.Message?.Key == transactionId.ToString(System.Globalization.CultureInfo.InvariantCulture))
            {
                return result;
            }
        }

        return null;
    }
}

