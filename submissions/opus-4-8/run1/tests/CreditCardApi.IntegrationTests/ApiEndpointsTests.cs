using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Confluent.Kafka;
using CreditCardApi.Application.Common;
using CreditCardApi.Application.CreditCards;
using CreditCardApi.Application.Transactions;
using FluentAssertions;
using Xunit;

namespace CreditCardApi.IntegrationTests;

public sealed class ApiEndpointsTests : IClassFixture<CreditCardApiFactory>
{
    private readonly CreditCardApiFactory _factory;
    private readonly HttpClient _client;

    public ApiEndpointsTests(CreditCardApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Health_returns_healthy()
    {
        var response = await _client.GetAsync("/health");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("status").GetString().Should().Be("healthy");
    }

    [Fact]
    public async Task Create_then_get_credit_card_masks_the_pan()
    {
        var created = await CreateCardAsync("Ada Lovelace", "4111111111111234", "VISA", 5000m);

        created.Id.Should().BeGreaterThan(0);
        created.CardNumberMasked.Should().EndWith("1234");
        created.CardNumberMasked.Should().NotContain("4111");

        var get = await _client.GetAsync($"/api/credit-cards/{created.Id}");
        get.StatusCode.Should().Be(HttpStatusCode.OK);
        var fetched = await get.Content.ReadFromJsonAsync<CreditCardResponse>();
        fetched!.CardholderName.Should().Be("Ada Lovelace");
    }

    [Fact]
    public async Task Create_credit_card_with_empty_cardholder_returns_400()
    {
        var response = await _client.PostAsJsonAsync("/api/credit-cards", new
        {
            cardholderName = "",
            cardNumber = "4111111111111234",
            creditLimit = 1000m,
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");
    }

    [Fact]
    public async Task Get_unknown_credit_card_returns_404()
    {
        var response = await _client.GetAsync("/api/credit-cards/999999");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");
    }

    [Fact]
    public async Task Create_transaction_returns_201_with_location_and_publishes_event()
    {
        var card = await CreateCardAsync("Grace Hopper", "5555444433331111", "MASTERCARD", 9000m);

        var response = await _client.PostAsJsonAsync("/api/transactions", new
        {
            creditCardId = card.Id,
            amount = 199.90m,
            merchant = "Amazon",
            category = "shopping",
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location.Should().NotBeNull();

        var created = await response.Content.ReadFromJsonAsync<TransactionResponse>();
        created!.Id.Should().BeGreaterThan(0);
        created.Amount.Should().Be(199.90m);

        // The created transaction must land on the Kafka topic (via the outbox).
        var published = ConsumeTransactionEvent(created.Id, TimeSpan.FromSeconds(40));
        published.Should().NotBeNull();
        published!.Value.GetProperty("merchant").GetString().Should().Be("Amazon");
    }

    [Fact]
    public async Task Create_transaction_for_unknown_card_returns_400()
    {
        var response = await _client.PostAsJsonAsync("/api/transactions", new
        {
            creditCardId = 987654,
            amount = 10m,
            merchant = "Amazon",
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Create_transaction_with_non_positive_amount_returns_400()
    {
        var card = await CreateCardAsync("Alan Turing", "4000000000000002", null, 100m);

        var response = await _client.PostAsJsonAsync("/api/transactions", new
        {
            creditCardId = card.Id,
            amount = 0m,
            merchant = "Amazon",
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task List_credit_cards_is_paginated()
    {
        await CreateCardAsync("Pager One", "4111111111110001", null, 100m);
        await CreateCardAsync("Pager Two", "4111111111110002", null, 100m);

        var response = await _client.GetAsync("/api/credit-cards?page=1&pageSize=1");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var page = await response.Content.ReadFromJsonAsync<PagedResult<CreditCardResponse>>();
        page!.PageSize.Should().Be(1);
        page.Items.Should().HaveCount(1);
        page.TotalCount.Should().BeGreaterThanOrEqualTo(2);
    }

    private async Task<CreditCardResponse> CreateCardAsync(string name, string pan, string? brand, decimal limit)
    {
        var response = await _client.PostAsJsonAsync("/api/credit-cards", new
        {
            cardholderName = name,
            cardNumber = pan,
            brand,
            creditLimit = limit,
        });
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return (await response.Content.ReadFromJsonAsync<CreditCardResponse>())!;
    }

    private JsonElement? ConsumeTransactionEvent(int transactionId, TimeSpan timeout)
    {
        var config = new ConsumerConfig
        {
            BootstrapServers = _factory.KafkaBootstrapServers,
            GroupId = $"itest-{Guid.NewGuid():N}",
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false,
        };

        using var consumer = new ConsumerBuilder<string, string>(config).Build();
        consumer.Subscribe(TransactionService.Topic);

        var deadline = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < deadline)
        {
            var result = consumer.Consume(TimeSpan.FromSeconds(2));
            if (result?.Message is null)
            {
                continue;
            }

            var element = JsonSerializer.Deserialize<JsonElement>(result.Message.Value);
            if (element.TryGetProperty("id", out var id) && id.GetInt32() == transactionId)
            {
                consumer.Close();
                return element;
            }
        }

        consumer.Close();
        return null;
    }
}
