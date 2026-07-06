using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace CreditCardApi.IntegrationTests;

[Collection(ApiCollectionDefinition.Name)]
public class TransactionsApiTests
{
    private readonly HttpClient _client;

    public TransactionsApiTests(ApiFixture fixture) => _client = fixture.Client;

    [Fact]
    public async Task Post_ValidTransaction_Returns201WithCamelCaseBody()
    {
        var card = await CreateCardAsync();

        var response = await _client.PostAsJsonAsync("/api/transactions", new
        {
            creditCardId = card.Id,
            amount = 199.90,
            merchant = "Amazon",
            category = "shopping",
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);

        // Assert the raw wire format: camelCase names, all contract fields present.
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = document.RootElement;
        Assert.True(root.GetProperty("id").GetInt32() > 0);
        Assert.Equal(card.Id, root.GetProperty("creditCardId").GetInt32());
        Assert.Equal(199.90m, root.GetProperty("amount").GetDecimal());
        Assert.Equal("Amazon", root.GetProperty("merchant").GetString());
        Assert.Equal("shopping", root.GetProperty("category").GetString());
        var createdAt = root.GetProperty("createdAt").GetDateTimeOffset();
        Assert.True(DateTimeOffset.UtcNow - createdAt < TimeSpan.FromMinutes(1));
    }

    [Fact]
    public async Task Post_UnknownCreditCard_Returns400Problem()
    {
        var response = await _client.PostAsJsonAsync("/api/transactions", new
        {
            creditCardId = 999999,
            amount = 10.00,
            merchant = "Amazon",
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Contains("999999", document.RootElement.GetProperty("detail").GetString(), StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-10.5)]
    public async Task Post_NonPositiveAmount_Returns400(double amount)
    {
        var card = await CreateCardAsync();

        var response = await _client.PostAsJsonAsync("/api/transactions", new
        {
            creditCardId = card.Id,
            amount,
            merchant = "Amazon",
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Post_BlankMerchant_Returns400(string merchant)
    {
        var card = await CreateCardAsync();

        var response = await _client.PostAsJsonAsync("/api/transactions", new
        {
            creditCardId = card.Id,
            amount = 10.00,
            merchant,
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Post_MissingAmount_Returns400()
    {
        var card = await CreateCardAsync();

        var response = await _client.PostAsJsonAsync("/api/transactions", new
        {
            creditCardId = card.Id,
            merchant = "Amazon",
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetById_ReturnsTheTransaction()
    {
        var created = await CreateTransactionAsync();

        var fetched = await _client.GetFromJsonAsync<TransactionDto>($"/api/transactions/{created.Id}");

        Assert.Equal(created, fetched);
    }

    [Fact]
    public async Task GetById_Unknown_Returns404()
    {
        var response = await _client.GetAsync("/api/transactions/999999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetAll_ReturnsArrayWithPaginationHeaders()
    {
        await CreateTransactionAsync();
        await CreateTransactionAsync();

        var response = await _client.GetAsync("/api/transactions?page=1&pageSize=1");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var transactions = await response.Content.ReadFromJsonAsync<List<TransactionDto>>();
        Assert.NotNull(transactions);
        Assert.Single(transactions);
        var totalCount = int.Parse(response.Headers.GetValues("X-Total-Count").Single(), System.Globalization.CultureInfo.InvariantCulture);
        Assert.True(totalCount >= 2);
    }

    [Fact]
    public async Task Put_ReplacesTheTransaction()
    {
        var created = await CreateTransactionAsync();

        var response = await _client.PutAsJsonAsync($"/api/transactions/{created.Id}", new
        {
            creditCardId = created.CreditCardId,
            amount = 42.42,
            merchant = "Bookstore",
            category = "books",
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = await response.Content.ReadFromJsonAsync<TransactionDto>();
        Assert.NotNull(updated);
        Assert.Equal(42.42m, updated.Amount);
        Assert.Equal("Bookstore", updated.Merchant);
        Assert.Equal("books", updated.Category);
    }

    [Fact]
    public async Task Put_Unknown_Returns404()
    {
        var card = await CreateCardAsync();

        var response = await _client.PutAsJsonAsync("/api/transactions/999999", new
        {
            creditCardId = card.Id,
            amount = 10.00,
            merchant = "Amazon",
        });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Put_MovingToUnknownCard_Returns400()
    {
        var created = await CreateTransactionAsync();

        var response = await _client.PutAsJsonAsync($"/api/transactions/{created.Id}", new
        {
            creditCardId = 999999,
            amount = 10.00,
            merchant = "Amazon",
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Delete_RemovesTheTransaction()
    {
        var created = await CreateTransactionAsync();

        var deleteResponse = await _client.DeleteAsync($"/api/transactions/{created.Id}");

        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await _client.GetAsync($"/api/transactions/{created.Id}")).StatusCode);
    }

    [Fact]
    public async Task Delete_Unknown_Returns404()
    {
        var response = await _client.DeleteAsync("/api/transactions/999999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Post_FortyConcurrentCreates_AllSucceedWithoutServerErrors()
    {
        var card = await CreateCardAsync();

        var responses = await Task.WhenAll(Enumerable.Range(1, 40).Select(i =>
            _client.PostAsJsonAsync("/api/transactions", new
            {
                creditCardId = card.Id,
                amount = i + 0.99,
                merchant = $"Merchant {i}",
            })));

        Assert.All(responses, response => Assert.Equal(HttpStatusCode.Created, response.StatusCode));

        var listResponse = await _client.GetAsync($"/api/credit-cards/{card.Id}/transactions?pageSize=100");
        var transactions = await listResponse.Content.ReadFromJsonAsync<List<TransactionDto>>();
        Assert.Equal(40, transactions!.Count);
    }

    private async Task<CardDto> CreateCardAsync()
    {
        var response = await _client.PostAsJsonAsync("/api/credit-cards", new
        {
            cardholderName = "Ada Lovelace",
            cardNumber = "4111111111111111",
            brand = "VISA",
            creditLimit = 5000.00,
        });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<CardDto>())!;
    }

    private async Task<TransactionDto> CreateTransactionAsync()
    {
        var card = await CreateCardAsync();
        var response = await _client.PostAsJsonAsync("/api/transactions", new
        {
            creditCardId = card.Id,
            amount = 19.99,
            merchant = "Grocery Store",
            category = "groceries",
        });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<TransactionDto>())!;
    }
}
