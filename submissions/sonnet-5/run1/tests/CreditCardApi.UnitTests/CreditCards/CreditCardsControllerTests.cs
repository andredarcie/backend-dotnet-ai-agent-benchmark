using CreditCardApi.Api.Controllers;
using CreditCardApi.Application.CreditCards;
using CreditCardApi.Application.CreditCards.Dtos;
using CreditCardApi.Application.Transactions.Dtos;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace CreditCardApi.UnitTests.CreditCards;

public class CreditCardsControllerTests
{
    private readonly Mock<ICreditCardService> _creditCardService = new();
    private readonly CreditCardsController _sut;

    public CreditCardsControllerTests()
    {
        _sut = new CreditCardsController(_creditCardService.Object);
    }

    [Fact]
    public async Task GetAll_ReturnsOkWithTheServiceResult()
    {
        var expected = new List<CreditCardResponse> { new(1, "Ada", "**** 1111", "VISA", 1000m, DateTime.UtcNow) };
        _creditCardService.Setup(s => s.GetPagedAsync(1, 20, It.IsAny<CancellationToken>())).ReturnsAsync(expected);

        var result = await _sut.GetAll(1, 20, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Same(expected, okResult.Value);
    }

    [Fact]
    public async Task GetById_ReturnsOkWithTheServiceResult()
    {
        var expected = new CreditCardResponse(1, "Ada", "**** 1111", "VISA", 1000m, DateTime.UtcNow);
        _creditCardService.Setup(s => s.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(expected);

        var result = await _sut.GetById(1, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Same(expected, okResult.Value);
    }

    [Fact]
    public async Task Create_ReturnsCreatedAtActionWithLocationAndBody()
    {
        var request = new CreateCreditCardRequest("Ada", "4111111111111111", "VISA", 1000m);
        var created = new CreditCardResponse(42, "Ada", "**** 1111", "VISA", 1000m, DateTime.UtcNow);
        _creditCardService.Setup(s => s.CreateAsync(request, It.IsAny<CancellationToken>())).ReturnsAsync(created);

        var result = await _sut.Create(request, CancellationToken.None);

        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        Assert.Equal(nameof(CreditCardsController.GetById), createdResult.ActionName);
        Assert.Equal(42, createdResult.RouteValues?["id"]);
        Assert.Same(created, createdResult.Value);
    }

    [Fact]
    public async Task GetTransactions_ReturnsOkWithTheServiceResult()
    {
        var expected = new List<TransactionResponse> { new(1, 1, 10m, "Acme", null, DateTime.UtcNow) };
        _creditCardService.Setup(s => s.GetTransactionsForCardAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(expected);

        var result = await _sut.GetTransactions(1, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Same(expected, okResult.Value);
    }
}
