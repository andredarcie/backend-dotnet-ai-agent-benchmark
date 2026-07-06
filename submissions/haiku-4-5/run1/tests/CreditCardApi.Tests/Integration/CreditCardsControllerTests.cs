using CreditCardApi.Application.DTOs;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace CreditCardApi.Tests.Integration;

public class CreditCardsControllerTests : IntegrationTestBase
{
    [Fact]
    public async Task Get_All_Returns_Empty_List_When_No_Cards()
    {
        var response = await Client.GetAsync("/api/credit-cards");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var cards = await response.Content.ReadFromJsonAsync<IEnumerable<CreditCardDto>>();
        Assert.NotNull(cards);
        Assert.Empty(cards);
    }

    [Fact]
    public async Task Create_Credit_Card_Returns_201_With_Valid_Data()
    {
        var request = new CreateCreditCardRequest
        {
            CardholderName = "John Doe",
            CardNumber = "4532123456789010",
            Brand = "VISA",
            CreditLimit = 5000
        };

        var response = await Client.PostAsJsonAsync("/api/credit-cards", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var card = await response.Content.ReadFromJsonAsync<CreditCardDto>();
        Assert.NotNull(card);
        Assert.Equal("John Doe", card.CardholderName);
        Assert.Equal("VISA", card.Brand);
        Assert.Equal(5000, card.CreditLimit);
    }

    [Fact]
    public async Task Create_Credit_Card_Returns_400_When_Name_Empty()
    {
        var request = new CreateCreditCardRequest
        {
            CardholderName = "",
            CardNumber = "4532123456789010",
            Brand = "VISA",
            CreditLimit = 5000
        };

        var response = await Client.PostAsJsonAsync("/api/credit-cards", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_Credit_Card_Returns_400_When_CardNumber_Empty()
    {
        var request = new CreateCreditCardRequest
        {
            CardholderName = "John Doe",
            CardNumber = "",
            Brand = "VISA",
            CreditLimit = 5000
        };

        var response = await Client.PostAsJsonAsync("/api/credit-cards", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Get_Credit_Card_By_Id_Returns_404_When_Not_Found()
    {
        var response = await Client.GetAsync("/api/credit-cards/999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Delete_Credit_Card_Returns_204()
    {
        var createRequest = new CreateCreditCardRequest
        {
            CardholderName = "Jane Doe",
            CardNumber = "5500123456789012",
            Brand = "MASTERCARD",
            CreditLimit = 10000
        };

        var createResponse = await Client.PostAsJsonAsync("/api/credit-cards", createRequest);
        var card = await createResponse.Content.ReadFromJsonAsync<CreditCardDto>();

        var deleteResponse = await Client.DeleteAsync($"/api/credit-cards/{card.Id}");

        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
    }
}
