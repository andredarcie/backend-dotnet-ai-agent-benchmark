using System.Net;
using System.Net.Http.Json;
using CreditCardApi.Application.CreditCards;
using CreditCardApi.Application.Transactions;

namespace CreditCardApi.IntegrationTests;

[Collection(ApiCollectionDefinition.Name)]
public class CreditCardsApiTests(ApiFixture fixture)
{
    private static CreditCardRequest ValidRequest(string cardholderName = "Ada Lovelace") => new()
    {
        CardholderName = cardholderName,
        CardNumber = "4111111111111111",
        Brand = "VISA",
        CreditLimit = 5000m,
    };

    [Fact]
    public async Task Post_Creates201WithLocationAndMaskedCardNumber()
    {
        using var client = fixture.CreateClient();

        var response = await client.PostAsJsonAsync("/api/credit-cards", ValidRequest(), Contracts.Json);
        var created = await response.Content.ReadFromJsonAsync<CreditCardResponse>(Contracts.Json);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);
        Assert.EndsWith($"/api/credit-cards/{created!.Id}", response.Headers.Location!.ToString());
        Assert.Equal("**** **** **** 1111", created.CardNumber);
        Assert.True(created.Id > 0);
    }

    [Theory]
    [InlineData("cardholderName", "")]
    [InlineData("cardNumber", "")]
    public async Task Post_Returns400ProblemJson_WhenRequiredFieldIsBlank(string field, string blankValue)
    {
        using var client = fixture.CreateClient();
        var payload = field == "cardholderName"
            ? ValidRequest(blankValue)
            : new CreditCardRequest { CardholderName = "Ada Lovelace", CardNumber = blankValue, CreditLimit = 100m };

        var response = await client.PostAsJsonAsync("/api/credit-cards", payload, Contracts.Json);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task GetById_ReturnsTheCreatedCard()
    {
        using var client = fixture.CreateClient();
        var created = await CreateCardAsync(client);

        var response = await client.GetAsync($"/api/credit-cards/{created.Id}");
        var fetched = await response.Content.ReadFromJsonAsync<CreditCardResponse>(Contracts.Json);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(created.Id, fetched!.Id);
        Assert.Equal(created.CardholderName, fetched.CardholderName);
    }

    [Fact]
    public async Task GetById_Returns404ProblemJson_WhenMissing()
    {
        using var client = fixture.CreateClient();

        var response = await client.GetAsync("/api/credit-cards/999999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task List_ReturnsPaginationHeaders()
    {
        using var client = fixture.CreateClient();
        await CreateCardAsync(client);

        var response = await client.GetAsync("/api/credit-cards?page=1&pageSize=1");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(response.Headers.Contains("X-Total-Count"));
        Assert.Equal("1", response.Headers.GetValues("X-Page-Size").Single());
    }

    [Fact]
    public async Task Put_UpdatesAnExistingCard()
    {
        using var client = fixture.CreateClient();
        var created = await CreateCardAsync(client);
        var update = new CreditCardRequest { CardholderName = "Ada L.", CardNumber = "5500005555555559", Brand = "MASTERCARD", CreditLimit = 7500m };

        var response = await client.PutAsJsonAsync($"/api/credit-cards/{created.Id}", update, Contracts.Json);
        var updated = await response.Content.ReadFromJsonAsync<CreditCardResponse>(Contracts.Json);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Ada L.", updated!.CardholderName);
        Assert.Equal("**** **** **** 5559", updated.CardNumber);
        Assert.Equal(7500m, updated.CreditLimit);
    }

    [Fact]
    public async Task Put_Returns404_WhenMissing()
    {
        using var client = fixture.CreateClient();

        var response = await client.PutAsJsonAsync("/api/credit-cards/999999", ValidRequest(), Contracts.Json);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Put_Returns400_WhenInvalid()
    {
        using var client = fixture.CreateClient();
        var created = await CreateCardAsync(client);
        var invalid = new CreditCardRequest { CardholderName = "", CardNumber = "4111111111111111", CreditLimit = 100m };

        var response = await client.PutAsJsonAsync($"/api/credit-cards/{created.Id}", invalid, Contracts.Json);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Delete_RemovesTheCardAndCascadesToItsTransactions()
    {
        using var client = fixture.CreateClient();
        var card = await CreateCardAsync(client);
        var transactionResponse = await client.PostAsJsonAsync(
            "/api/transactions",
            new TransactionRequest { CreditCardId = card.Id, Amount = 10m, Merchant = "Test" },
            Contracts.Json);
        var transaction = await transactionResponse.Content.ReadFromJsonAsync<TransactionResponse>(Contracts.Json);

        var deleteResponse = await client.DeleteAsync($"/api/credit-cards/{card.Id}");
        var getCardResponse = await client.GetAsync($"/api/credit-cards/{card.Id}");
        var getTransactionResponse = await client.GetAsync($"/api/transactions/{transaction!.Id}");

        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, getCardResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, getTransactionResponse.StatusCode);
    }

    [Fact]
    public async Task Delete_Returns404_WhenMissing()
    {
        using var client = fixture.CreateClient();

        var response = await client.DeleteAsync("/api/credit-cards/999999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetTransactions_ReturnsOnlyTransactionsForThatCard()
    {
        using var client = fixture.CreateClient();
        var cardA = await CreateCardAsync(client);
        var cardB = await CreateCardAsync(client);
        await client.PostAsJsonAsync("/api/transactions", new TransactionRequest { CreditCardId = cardA.Id, Amount = 10m, Merchant = "A" }, Contracts.Json);
        await client.PostAsJsonAsync("/api/transactions", new TransactionRequest { CreditCardId = cardB.Id, Amount = 20m, Merchant = "B" }, Contracts.Json);

        var response = await client.GetAsync($"/api/credit-cards/{cardA.Id}/transactions");
        var transactions = await response.Content.ReadFromJsonAsync<List<TransactionResponse>>(Contracts.Json);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.All(transactions!, t => Assert.Equal(cardA.Id, t.CreditCardId));
    }

    [Fact]
    public async Task GetTransactions_Returns404_WhenCardMissing()
    {
        using var client = fixture.CreateClient();

        var response = await client.GetAsync("/api/credit-cards/999999/transactions");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private static async Task<CreditCardResponse> CreateCardAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync("/api/credit-cards", ValidRequest(Guid.NewGuid().ToString()), Contracts.Json);
        return (await response.Content.ReadFromJsonAsync<CreditCardResponse>(Contracts.Json))!;
    }
}
