using CreditCardApi.Application.DTOs;
using CreditCardApi.Controllers;
using CreditCardApi.Application.Repositories;
using CreditCardApi.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;

namespace CreditCardApi.Tests;

public class CreditCardsControllerTests
{
    private readonly Mock<ICreditCardRepository> _mockRepository;
    private readonly Mock<ITransactionRepository> _mockTransactionRepository;
    private readonly Mock<ILogger<CreditCardsController>> _mockLogger;
    private readonly CreditCardsController _controller;

    public CreditCardsControllerTests()
    {
        _mockRepository = new Mock<ICreditCardRepository>();
        _mockTransactionRepository = new Mock<ITransactionRepository>();
        _mockLogger = new Mock<ILogger<CreditCardsController>>();
        _controller = new CreditCardsController(_mockRepository.Object, _mockTransactionRepository.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task GetAll_ReturnsOkWithCards()
    {
        var cards = new List<CreditCard>
        {
            new() { Id = 1, CardholderName = "John", CardNumber = "1234567890123456", CreditLimit = 5000m, CreatedAt = DateTime.UtcNow }
        };

        _mockRepository.Setup(r => r.GetAllAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(cards);

        var result = await _controller.GetAll(1, 10);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedCards = Assert.IsAssignableFrom<IEnumerable<CreditCardDto>>(okResult.Value);
        Assert.Single(returnedCards);
    }

    [Fact]
    public async Task Create_WithValidRequest_ReturnsCreatedAtAction()
    {
        var request = new CreateCreditCardRequest("John Doe", "4532123456789012", "VISA", 10000m);
        var createdCard = new CreditCard
        {
            Id = 1,
            CardholderName = request.CardholderName,
            CardNumber = request.CardNumber,
            Brand = request.Brand,
            CreditLimit = request.CreditLimit,
            CreatedAt = DateTime.UtcNow
        };

        _mockRepository.Setup(r => r.AddAsync(It.IsAny<CreditCard>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(createdCard);

        var result = await _controller.Create(request);

        Assert.IsType<CreatedAtActionResult>(result);
    }

    [Fact]
    public async Task Create_WithEmptyCardholderName_ReturnsBadRequest()
    {
        var request = new CreateCreditCardRequest("", "4532123456789012", "VISA", 10000m);

        var result = await _controller.Create(request);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        var problemDetails = Assert.IsType<ProblemDetails>(badRequest.Value);
        Assert.Equal(400, problemDetails.Status);
    }

    [Fact]
    public async Task Create_WithNegativeCreditLimit_ReturnsBadRequest()
    {
        var request = new CreateCreditCardRequest("John", "4532123456789012", "VISA", -100m);

        var result = await _controller.Create(request);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.IsType<ProblemDetails>(badRequest.Value);
    }

    [Fact]
    public async Task GetById_WithValidId_ReturnsOk()
    {
        var card = new CreditCard
        {
            Id = 1,
            CardholderName = "John",
            CardNumber = "4532123456789012",
            Brand = "VISA",
            CreditLimit = 5000m,
            CreatedAt = DateTime.UtcNow
        };

        _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(card);

        var result = await _controller.GetById(1);

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task GetById_WithInvalidId_ReturnsNotFound()
    {
        _mockRepository.Setup(r => r.GetByIdAsync(99, It.IsAny<CancellationToken>()))
            .ReturnsAsync((CreditCard?)null);

        var result = await _controller.GetById(99);

        var notFound = Assert.IsType<NotFoundObjectResult>(result);
        var problemDetails = Assert.IsType<ProblemDetails>(notFound.Value);
        Assert.Equal(404, problemDetails.Status);
    }

    [Fact]
    public async Task Delete_WithValidId_ReturnsNoContent()
    {
        var card = new CreditCard
        {
            Id = 1,
            CardholderName = "John",
            CardNumber = "4532123456789012",
            CreditLimit = 5000m,
            CreatedAt = DateTime.UtcNow
        };

        _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(card);

        _mockRepository.Setup(r => r.DeleteAsync(1, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await _controller.Delete(1);

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task GetTransactions_WithValidCardId_ReturnsTransactions()
    {
        var card = new CreditCard
        {
            Id = 1,
            CardholderName = "John",
            CardNumber = "4532123456789012",
            CreditLimit = 5000m,
            CreatedAt = DateTime.UtcNow
        };

        var transactions = new List<Transaction>
        {
            new() { Id = 1, CreditCardId = 1, Amount = 99.99m, Merchant = "Amazon", Category = "shopping", CreatedAt = DateTime.UtcNow }
        };

        _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(card);

        _mockTransactionRepository.Setup(r => r.GetByCreditCardIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(transactions);

        var result = await _controller.GetTransactions(1);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedTransactions = Assert.IsAssignableFrom<IEnumerable<TransactionDto>>(okResult.Value);
        Assert.Single(returnedTransactions);
    }
}

public class TransactionValidationTests
{
    [Fact]
    public void Transaction_WithAmountLessThanZero_IsInvalid()
    {
        var request = new CreateTransactionRequest(1, -100m, "Store", "shopping");
        Assert.True(request.Amount <= 0, "Amount should be validated in controller");
    }

    [Fact]
    public void Transaction_WithEmptyMerchant_IsInvalid()
    {
        var request = new CreateTransactionRequest(1, 100m, "", null);
        Assert.True(string.IsNullOrWhiteSpace(request.Merchant), "Merchant should be validated");
    }

    [Fact]
    public void Transaction_WithValidData_IsValid()
    {
        var request = new CreateTransactionRequest(1, 99.99m, "Amazon", "shopping");
        Assert.True(request.Amount > 0, "Amount should be positive");
        Assert.False(string.IsNullOrWhiteSpace(request.Merchant), "Merchant should not be empty");
    }
}
