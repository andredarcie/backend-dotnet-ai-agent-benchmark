using System.Net;
using System.Net.Http.Json;

namespace CreditCardApi.IntegrationTests;

[Collection(ApiCollectionDefinition.Name)]
public class CreditCardsApiTests
{
    private readonly HttpClient _client;

    public CreditCardsApiTests(ApiFixture fixture) => _client = fixture.Client;

    private static object ValidCard(string cardholderName = "Ada Lovelace") => new
    {
        cardholderName,
        cardNumber = "4111111111111111",
        brand = "VISA",
        creditLimit = 5000.00,
    };

    [Fact]
    public async Task Post_ValidCard_Returns201WithLocationAndBody()
    {
        var response = await _client.PostAsJsonAsync("/api/credit-cards", ValidCard());

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var card = await response.Content.ReadFromJsonAsync<CardDto>();
        Assert.NotNull(card);
        Assert.True(card.Id > 0);
        Assert.Equal("Ada Lovelace", card.CardholderName);
        Assert.Equal("VISA", card.Brand);
        Assert.Equal(5000.00m, card.CreditLimit);
        Assert.Equal(DateTimeKind.Utc, card.CreatedAt.ToUniversalTime().Kind);
        Assert.NotNull(response.Headers.Location);
        Assert.EndsWith($"/api/credit-cards/{card.Id}", response.Headers.Location.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Post_NeverEchoesTheFullCardNumber()
    {
        var response = await _client.PostAsJsonAsync("/api/credit-cards", ValidCard());
        var raw = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.DoesNotContain("4111111111111111", raw, StringComparison.Ordinal);
        var card = await response.Content.ReadFromJsonAsync<CardDto>();
        Assert.Equal("**** **** **** 1111", card!.CardNumber);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Post_BlankCardholderName_Returns400Problem(string cardholderName)
    {
        var response = await _client.PostAsJsonAsync("/api/credit-cards", new
        {
            cardholderName,
            cardNumber = "4111111111111111",
            creditLimit = 100.0,
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Post_EmptyCardNumber_Returns400()
    {
        var response = await _client.PostAsJsonAsync("/api/credit-cards", new
        {
            cardholderName = "Ada Lovelace",
            cardNumber = "",
            creditLimit = 100.0,
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Post_NegativeCreditLimit_Returns400()
    {
        var response = await _client.PostAsJsonAsync("/api/credit-cards", new
        {
            cardholderName = "Ada Lovelace",
            cardNumber = "4111111111111111",
            creditLimit = -1.0,
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetById_ReturnsTheCard()
    {
        var created = await CreateCardAsync();

        var fetched = await _client.GetFromJsonAsync<CardDto>($"/api/credit-cards/{created.Id}");

        Assert.Equal(created, fetched);
    }

    [Fact]
    public async Task GetById_Unknown_Returns404Problem()
    {
        var response = await _client.GetAsync("/api/credit-cards/999999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task GetAll_ReturnsArrayWithPaginationHeaders()
    {
        for (var i = 0; i < 3; i++)
        {
            await CreateCardAsync();
        }

        var response = await _client.GetAsync("/api/credit-cards?page=1&pageSize=2");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var cards = await response.Content.ReadFromJsonAsync<List<CardDto>>();
        Assert.NotNull(cards);
        Assert.Equal(2, cards.Count);
        var totalCount = int.Parse(response.Headers.GetValues("X-Total-Count").Single(), System.Globalization.CultureInfo.InvariantCulture);
        Assert.True(totalCount >= 3);
        Assert.Equal("2", response.Headers.GetValues("X-Page-Size").Single());
    }

    [Theory]
    [InlineData("page=0")]
    [InlineData("pageSize=0")]
    [InlineData("pageSize=101")]
    public async Task GetAll_InvalidPagination_Returns400(string query)
    {
        var response = await _client.GetAsync($"/api/credit-cards?{query}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Put_ReplacesTheCard()
    {
        var created = await CreateCardAsync();

        var response = await _client.PutAsJsonAsync($"/api/credit-cards/{created.Id}", new
        {
            cardholderName = "Grace Hopper",
            cardNumber = "5500000000000004",
            brand = "MASTERCARD",
            creditLimit = 7500.00,
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = await response.Content.ReadFromJsonAsync<CardDto>();
        Assert.NotNull(updated);
        Assert.Equal("Grace Hopper", updated.CardholderName);
        Assert.Equal("**** **** **** 0004", updated.CardNumber);
        Assert.Equal(7500.00m, updated.CreditLimit);
    }

    [Fact]
    public async Task Put_Unknown_Returns404()
    {
        var response = await _client.PutAsJsonAsync("/api/credit-cards/999999", ValidCard());

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Put_InvalidBody_Returns400()
    {
        var created = await CreateCardAsync();

        var response = await _client.PutAsJsonAsync($"/api/credit-cards/{created.Id}", new
        {
            cardholderName = "",
            cardNumber = "4111111111111111",
            creditLimit = 100.0,
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Delete_RemovesTheCardAndItsTransactions()
    {
        var card = await CreateCardAsync();
        var transactionResponse = await _client.PostAsJsonAsync("/api/transactions", new
        {
            creditCardId = card.Id,
            amount = 10.00,
            merchant = "Corner Shop",
        });
        var transaction = await transactionResponse.Content.ReadFromJsonAsync<TransactionDto>();

        var deleteResponse = await _client.DeleteAsync($"/api/credit-cards/{card.Id}");

        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await _client.GetAsync($"/api/credit-cards/{card.Id}")).StatusCode);
        // FK cascade: the card's transactions are gone as well.
        Assert.Equal(HttpStatusCode.NotFound, (await _client.GetAsync($"/api/transactions/{transaction!.Id}")).StatusCode);
    }

    [Fact]
    public async Task Delete_Unknown_Returns404()
    {
        var response = await _client.DeleteAsync("/api/credit-cards/999999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetTransactions_UnknownCard_Returns404()
    {
        var response = await _client.GetAsync("/api/credit-cards/999999/transactions");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetTransactions_ReturnsOnlyTheCardsTransactions()
    {
        var card = await CreateCardAsync();
        var otherCard = await CreateCardAsync();
        await _client.PostAsJsonAsync("/api/transactions", new { creditCardId = card.Id, amount = 12.34, merchant = "A" });
        await _client.PostAsJsonAsync("/api/transactions", new { creditCardId = otherCard.Id, amount = 56.78, merchant = "B" });

        var response = await _client.GetAsync($"/api/credit-cards/{card.Id}/transactions");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var transactions = await response.Content.ReadFromJsonAsync<List<TransactionDto>>();
        Assert.NotNull(transactions);
        var transaction = Assert.Single(transactions);
        Assert.Equal(card.Id, transaction.CreditCardId);
        Assert.Equal(12.34m, transaction.Amount);
    }

    private async Task<CardDto> CreateCardAsync()
    {
        var response = await _client.PostAsJsonAsync("/api/credit-cards", ValidCard());
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<CardDto>())!;
    }
}
