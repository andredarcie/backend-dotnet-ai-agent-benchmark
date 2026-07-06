using CreditCardApi.Application.DTOs;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace CreditCardApi.Tests.Integration;

public class TransactionsControllerTests : IntegrationTestBase
{
    [Fact]
    public async Task Get_All_Transactions_Returns_Empty_List_When_No_Transactions()
    {
        var response = await Client.GetAsync("/api/transactions");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var transactions = await response.Content.ReadFromJsonAsync<IEnumerable<TransactionDto>>();
        Assert.NotNull(transactions);
        Assert.Empty(transactions);
    }

    [Fact]
    public async Task Create_Transaction_Returns_400_When_Amount_Is_Zero()
    {
        var cardRequest = new CreateCreditCardRequest
        {
            CardholderName = "Test User",
            CardNumber = "4532123456789010",
            CreditLimit = 5000
        };

        var cardResponse = await Client.PostAsJsonAsync("/api/credit-cards", cardRequest);
        var card = await cardResponse.Content.ReadFromJsonAsync<CreditCardDto>();

        var transactionRequest = new CreateTransactionRequest
        {
            CreditCardId = card!.Id,
            Amount = 0,
            Merchant = "Amazon"
        };

        var response = await Client.PostAsJsonAsync("/api/transactions", transactionRequest);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_Transaction_Returns_400_When_Amount_Is_Negative()
    {
        var cardRequest = new CreateCreditCardRequest
        {
            CardholderName = "Test User",
            CardNumber = "4532123456789010",
            CreditLimit = 5000
        };

        var cardResponse = await Client.PostAsJsonAsync("/api/credit-cards", cardRequest);
        var card = await cardResponse.Content.ReadFromJsonAsync<CreditCardDto>();

        var transactionRequest = new CreateTransactionRequest
        {
            CreditCardId = card!.Id,
            Amount = -50,
            Merchant = "Amazon"
        };

        var response = await Client.PostAsJsonAsync("/api/transactions", transactionRequest);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_Transaction_Returns_400_When_Merchant_Empty()
    {
        var cardRequest = new CreateCreditCardRequest
        {
            CardholderName = "Test User",
            CardNumber = "4532123456789010",
            CreditLimit = 5000
        };

        var cardResponse = await Client.PostAsJsonAsync("/api/credit-cards", cardRequest);
        var card = await cardResponse.Content.ReadFromJsonAsync<CreditCardDto>();

        var transactionRequest = new CreateTransactionRequest
        {
            CreditCardId = card!.Id,
            Amount = 99.99m,
            Merchant = ""
        };

        var response = await Client.PostAsJsonAsync("/api/transactions", transactionRequest);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_Transaction_Returns_400_When_CreditCardId_Not_Found()
    {
        var transactionRequest = new CreateTransactionRequest
        {
            CreditCardId = 999,
            Amount = 99.99m,
            Merchant = "Amazon"
        };

        var response = await Client.PostAsJsonAsync("/api/transactions", transactionRequest);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_Transaction_Returns_201_With_Valid_Data()
    {
        var cardRequest = new CreateCreditCardRequest
        {
            CardholderName = "Test User",
            CardNumber = "4532123456789010",
            CreditLimit = 5000
        };

        var cardResponse = await Client.PostAsJsonAsync("/api/credit-cards", cardRequest);
        var card = await cardResponse.Content.ReadFromJsonAsync<CreditCardDto>();

        var transactionRequest = new CreateTransactionRequest
        {
            CreditCardId = card!.Id,
            Amount = 199.90m,
            Merchant = "Amazon",
            Category = "Shopping"
        };

        var response = await Client.PostAsJsonAsync("/api/transactions", transactionRequest);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var transaction = await response.Content.ReadFromJsonAsync<TransactionDto>();
        Assert.NotNull(transaction);
        Assert.Equal(card.Id, transaction.CreditCardId);
        Assert.Equal(199.90m, transaction.Amount);
        Assert.Equal("Amazon", transaction.Merchant);
        Assert.Equal("Shopping", transaction.Category);
    }

    [Fact]
    public async Task Get_Transaction_By_Id_Returns_404_When_Not_Found()
    {
        var response = await Client.GetAsync("/api/transactions/999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Delete_Transaction_Returns_204()
    {
        var cardRequest = new CreateCreditCardRequest
        {
            CardholderName = "Test User",
            CardNumber = "4532123456789010",
            CreditLimit = 5000
        };

        var cardResponse = await Client.PostAsJsonAsync("/api/credit-cards", cardRequest);
        var card = await cardResponse.Content.ReadFromJsonAsync<CreditCardDto>();

        var transactionRequest = new CreateTransactionRequest
        {
            CreditCardId = card!.Id,
            Amount = 99.99m,
            Merchant = "Amazon"
        };

        var transactionResponse = await Client.PostAsJsonAsync("/api/transactions", transactionRequest);
        var transaction = await transactionResponse.Content.ReadFromJsonAsync<TransactionDto>();

        var deleteResponse = await Client.DeleteAsync($"/api/transactions/{transaction!.Id}");

        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
    }
}
