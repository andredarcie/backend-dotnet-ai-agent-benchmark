using CreditCardApi.Application.Common.Exceptions;
using CreditCardApi.Application.CreditCards;
using CreditCardApi.Application.Transactions;
using CreditCardApi.Application.Transactions.Dtos;
using CreditCardApi.Domain.Entities;
using CreditCardApi.UnitTests.TestSupport;
using Moq;

namespace CreditCardApi.UnitTests.Transactions;

public class TransactionServiceTests
{
    private readonly Mock<ITransactionRepository> _transactionRepository = new();
    private readonly Mock<ICreditCardRepository> _creditCardRepository = new();
    private readonly Mock<ITransactionEventPublisher> _eventPublisher = new();
    private readonly TransactionService _sut;

    public TransactionServiceTests()
    {
        var timeProvider = new FixedTimeProvider(new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero));
        _creditCardRepository.Setup(r => r.ExistsAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _sut = new TransactionService(_transactionRepository.Object, _creditCardRepository.Object, _eventPublisher.Object, timeProvider);
    }

    [Fact]
    public async Task CreateAsync_WithValidRequest_PersistsThenPublishes()
    {
        var request = new CreateTransactionRequest(1, 199.90m, "Amazon", "shopping");
        var callOrder = new List<string>();

        _transactionRepository
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Callback(() => callOrder.Add("save"))
            .Returns(Task.CompletedTask);
        _eventPublisher
            .Setup(p => p.PublishCreatedAsync(It.IsAny<TransactionResponse>(), It.IsAny<CancellationToken>()))
            .Callback(() => callOrder.Add("publish"))
            .Returns(Task.CompletedTask);

        var response = await _sut.CreateAsync(request, CancellationToken.None);

        Assert.Equal(["save", "publish"], callOrder);
        Assert.Equal(1, response.CreditCardId);
        Assert.Equal(199.90m, response.Amount);
        Assert.Equal("Amazon", response.Merchant);
        Assert.Equal("shopping", response.Category);
        _eventPublisher.Verify(p => p.PublishCreatedAsync(It.IsAny<TransactionResponse>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public async Task CreateAsync_WithNonPositiveAmount_ThrowsValidationException(decimal amount)
    {
        var request = new CreateTransactionRequest(1, amount, "Acme", null);

        var exception = await Assert.ThrowsAsync<ValidationException>(() => _sut.CreateAsync(request, CancellationToken.None));

        Assert.Contains("amount", exception.Errors.Keys);
        _transactionRepository.Verify(r => r.Add(It.IsAny<Transaction>()), Times.Never);
        _eventPublisher.Verify(p => p.PublishCreatedAsync(It.IsAny<TransactionResponse>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task CreateAsync_WithEmptyMerchant_ThrowsValidationException(string merchant)
    {
        var request = new CreateTransactionRequest(1, 10m, merchant, null);

        var exception = await Assert.ThrowsAsync<ValidationException>(() => _sut.CreateAsync(request, CancellationToken.None));

        Assert.Contains("merchant", exception.Errors.Keys);
    }

    [Fact]
    public async Task CreateAsync_WithNonExistentCreditCard_ThrowsValidationException()
    {
        _creditCardRepository.Setup(r => r.ExistsAsync(999, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        var request = new CreateTransactionRequest(999, 10m, "Acme", null);

        var exception = await Assert.ThrowsAsync<ValidationException>(() => _sut.CreateAsync(request, CancellationToken.None));

        Assert.Contains("creditCardId", exception.Errors.Keys);
    }

    [Fact]
    public async Task CreateAsync_WithAllFieldsInvalid_ReportsEveryError()
    {
        _creditCardRepository.Setup(r => r.ExistsAsync(999, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        var request = new CreateTransactionRequest(999, -1m, "", null);

        var exception = await Assert.ThrowsAsync<ValidationException>(() => _sut.CreateAsync(request, CancellationToken.None));

        Assert.Equal(3, exception.Errors.Count);
        Assert.Contains("amount", exception.Errors.Keys);
        Assert.Contains("merchant", exception.Errors.Keys);
        Assert.Contains("creditCardId", exception.Errors.Keys);
    }

    [Fact]
    public async Task GetByIdAsync_WhenTransactionExists_ReturnsResponse()
    {
        var transaction = new Transaction { Id = 1, CreditCardId = 1, Amount = 10m, Merchant = "Acme", CreatedAt = DateTime.UtcNow };
        _transactionRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(transaction);

        var response = await _sut.GetByIdAsync(1, CancellationToken.None);

        Assert.Equal(1, response.Id);
    }

    [Fact]
    public async Task GetByIdAsync_WhenTransactionMissing_ThrowsNotFoundException()
    {
        _transactionRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync((Transaction?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => _sut.GetByIdAsync(999, CancellationToken.None));
    }

    [Theory]
    [InlineData(0, 0, 1, 20)]
    [InlineData(2, 500, 2, 100)]
    public async Task GetPagedAsync_NormalizesPagingParameters(int pageNumber, int pageSize, int expectedPageNumber, int expectedPageSize)
    {
        _transactionRepository
            .Setup(r => r.GetPagedAsync(expectedPageNumber, expectedPageSize, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        await _sut.GetPagedAsync(pageNumber, pageSize, CancellationToken.None);

        _transactionRepository.Verify(r => r.GetPagedAsync(expectedPageNumber, expectedPageSize, It.IsAny<CancellationToken>()), Times.Once);
    }
}
