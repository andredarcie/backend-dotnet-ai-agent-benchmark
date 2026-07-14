using CreditCardApi.Api.Controllers;
using CreditCardApi.Application.Transactions;
using CreditCardApi.Application.Transactions.Dtos;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace CreditCardApi.UnitTests.Transactions;

public class TransactionsControllerTests
{
    private readonly Mock<ITransactionService> _transactionService = new();
    private readonly TransactionsController _sut;

    public TransactionsControllerTests()
    {
        _sut = new TransactionsController(_transactionService.Object);
    }

    [Fact]
    public async Task GetAll_ReturnsOkWithTheServiceResult()
    {
        var expected = new List<TransactionResponse> { new(1, 1, 10m, "Acme", null, DateTime.UtcNow) };
        _transactionService.Setup(s => s.GetPagedAsync(1, 20, It.IsAny<CancellationToken>())).ReturnsAsync(expected);

        var result = await _sut.GetAll(1, 20, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Same(expected, okResult.Value);
    }

    [Fact]
    public async Task GetById_ReturnsOkWithTheServiceResult()
    {
        var expected = new TransactionResponse(1, 1, 10m, "Acme", null, DateTime.UtcNow);
        _transactionService.Setup(s => s.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(expected);

        var result = await _sut.GetById(1, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Same(expected, okResult.Value);
    }

    [Fact]
    public async Task Create_ReturnsCreatedAtActionWithLocationAndBody()
    {
        var request = new CreateTransactionRequest(1, 199.90m, "Amazon", "shopping");
        var created = new TransactionResponse(7, 1, 199.90m, "Amazon", "shopping", DateTime.UtcNow);
        _transactionService.Setup(s => s.CreateAsync(request, It.IsAny<CancellationToken>())).ReturnsAsync(created);

        var result = await _sut.Create(request, CancellationToken.None);

        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        Assert.Equal(nameof(TransactionsController.GetById), createdResult.ActionName);
        Assert.Equal(7, createdResult.RouteValues?["id"]);
        Assert.Same(created, createdResult.Value);
    }
}
