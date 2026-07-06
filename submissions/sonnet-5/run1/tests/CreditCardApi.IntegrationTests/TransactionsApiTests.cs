using System.Net;
using System.Net.Http.Json;
using CreditCardApi.Application.CreditCards;
using CreditCardApi.Application.Transactions;

namespace CreditCardApi.IntegrationTests;

[Collection(ApiCollectionDefinition.Name)]
public class TransactionsApiTests(ApiFixture fixture)
{
    [Fact]
    public async Task Post_Creates201WithLocation()
    {
        using var client = fixture.CreateClient();
        var card = await CreateCardAsync(client);

        var response = await client.PostAsJsonAsync(
            "/api/transactions",
            new TransactionRequest { CreditCardId = card.Id, Amount = 199.90m, Merchant = "Amazon", Category = "shopping" },
            Contracts.Json);
        var created = await response.Content.ReadFromJsonAsync<TransactionResponse>(Contracts.Json);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);
        Assert.EndsWith($"/api/transactions/{created!.Id}", response.Headers.Location!.ToString());
        Assert.Equal(199.90m, created.Amount);
        Assert.Equal(card.Id, created.CreditCardId);
    }

    [Fact]
    public async Task Post_Returns400_WhenAmountIsNotPositive()
    {
        using var client = fixture.CreateClient();
        var card = await CreateCardAsync(client);

        var response = await client.PostAsJsonAsync(
            "/api/transactions", new TransactionRequest { CreditCardId = card.Id, Amount = 0m, Merchant = "Amazon" }, Contracts.Json);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Post_Returns400_WhenMerchantIsBlank()
    {
        using var client = fixture.CreateClient();
        var card = await CreateCardAsync(client);

        var response = await client.PostAsJsonAsync(
            "/api/transactions", new TransactionRequest { CreditCardId = card.Id, Amount = 10m, Merchant = "" }, Contracts.Json);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Post_Returns400_WhenCreditCardDoesNotExist()
    {
        using var client = fixture.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/transactions", new TransactionRequest { CreditCardId = 999999, Amount = 10m, Merchant = "Amazon" }, Contracts.Json);
        var problem = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.Contains("999999", problem);
    }

    [Fact]
    public async Task GetById_Returns404_WhenMissing()
    {
        using var client = fixture.CreateClient();

        var response = await client.GetAsync("/api/transactions/999999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Put_UpdatesAmountMerchantAndCategory()
    {
        using var client = fixture.CreateClient();
        var card = await CreateCardAsync(client);
        var created = await CreateTransactionAsync(client, card.Id);

        var response = await client.PutAsJsonAsync(
            $"/api/transactions/{created.Id}",
            new TransactionRequest { CreditCardId = card.Id, Amount = 55m, Merchant = "eBay", Category = "electronics" },
            Contracts.Json);
        var updated = await response.Content.ReadFromJsonAsync<TransactionResponse>(Contracts.Json);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(55m, updated!.Amount);
        Assert.Equal("eBay", updated.Merchant);
        Assert.Equal("electronics", updated.Category);
    }

    [Fact]
    public async Task Put_Returns400_WhenMovingToADifferentCard()
    {
        using var client = fixture.CreateClient();
        var cardA = await CreateCardAsync(client);
        var cardB = await CreateCardAsync(client);
        var created = await CreateTransactionAsync(client, cardA.Id);

        var response = await client.PutAsJsonAsync(
            $"/api/transactions/{created.Id}",
            new TransactionRequest { CreditCardId = cardB.Id, Amount = 10m, Merchant = "X" },
            Contracts.Json);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Put_Returns404_WhenMissing()
    {
        using var client = fixture.CreateClient();
        var card = await CreateCardAsync(client);

        var response = await client.PutAsJsonAsync(
            "/api/transactions/999999",
            new TransactionRequest { CreditCardId = card.Id, Amount = 10m, Merchant = "X" },
            Contracts.Json);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Delete_RemovesTheTransaction()
    {
        using var client = fixture.CreateClient();
        var card = await CreateCardAsync(client);
        var created = await CreateTransactionAsync(client, card.Id);

        var deleteResponse = await client.DeleteAsync($"/api/transactions/{created.Id}");
        var getResponse = await client.GetAsync($"/api/transactions/{created.Id}");

        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    [Fact]
    public async Task Delete_Returns404_WhenMissing()
    {
        using var client = fixture.CreateClient();

        var response = await client.DeleteAsync("/api/transactions/999999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task ConcurrentCreates_AllSucceedWithoutServerErrors()
    {
        using var client = fixture.CreateClient();
        var card = await CreateCardAsync(client);

        var responses = await Task.WhenAll(Enumerable.Range(0, 40).Select(i => client.PostAsJsonAsync(
            "/api/transactions",
            new TransactionRequest { CreditCardId = card.Id, Amount = 1m + i, Merchant = $"Concurrent-{i}" },
            Contracts.Json)));

        Assert.All(responses, r => Assert.Equal(HttpStatusCode.Created, r.StatusCode));
        var ids = await Task.WhenAll(responses.Select(async r => (await r.Content.ReadFromJsonAsync<TransactionResponse>(Contracts.Json))!.Id));
        Assert.Equal(40, ids.Distinct().Count());
    }

    private static async Task<CreditCardResponse> CreateCardAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync(
            "/api/credit-cards",
            new CreditCardRequest { CardholderName = Guid.NewGuid().ToString(), CardNumber = "4111111111111111", CreditLimit = 5000m },
            Contracts.Json);
        return (await response.Content.ReadFromJsonAsync<CreditCardResponse>(Contracts.Json))!;
    }

    private static async Task<TransactionResponse> CreateTransactionAsync(HttpClient client, int creditCardId)
    {
        var response = await client.PostAsJsonAsync(
            "/api/transactions", new TransactionRequest { CreditCardId = creditCardId, Amount = 10m, Merchant = "Test" }, Contracts.Json);
        return (await response.Content.ReadFromJsonAsync<TransactionResponse>(Contracts.Json))!;
    }
}
