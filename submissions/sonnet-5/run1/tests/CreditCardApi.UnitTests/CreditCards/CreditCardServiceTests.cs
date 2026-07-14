using CreditCardApi.Application.Common.Exceptions;
using CreditCardApi.Application.CreditCards;
using CreditCardApi.Application.CreditCards.Dtos;
using CreditCardApi.Application.Transactions;
using CreditCardApi.Domain.Entities;
using CreditCardApi.UnitTests.TestSupport;
using Moq;

namespace CreditCardApi.UnitTests.CreditCards;

public class CreditCardServiceTests
{
    private readonly Mock<ICreditCardRepository> _creditCardRepository = new();
    private readonly Mock<ITransactionRepository> _transactionRepository = new();
    private readonly CreditCardService _sut;

    public CreditCardServiceTests()
    {
        var timeProvider = new FixedTimeProvider(new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero));
        _sut = new CreditCardService(_creditCardRepository.Object, _transactionRepository.Object, timeProvider);
    }

    [Fact]
    public async Task CreateAsync_WithValidRequest_PersistsAndReturnsMaskedResponse()
    {
        var request = new CreateCreditCardRequest("Ada Lovelace", "4111111111111111", "VISA", 5000m);

        CreditCard? added = null;
        _creditCardRepository
            .Setup(r => r.Add(It.IsAny<CreditCard>()))
            .Callback<CreditCard>(c => added = c);

        var response = await _sut.CreateAsync(request, CancellationToken.None);

        Assert.NotNull(added);
        Assert.Equal("4111111111111111", added.CardNumber);
        _creditCardRepository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

        Assert.Equal("Ada Lovelace", response.CardholderName);
        Assert.Equal("VISA", response.Brand);
        Assert.Equal(5000m, response.CreditLimit);
        Assert.DoesNotContain("4111111111111111", response.CardNumber, StringComparison.Ordinal);
        Assert.EndsWith("1111", response.CardNumber, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("", "4111111111111111")]
    [InlineData("   ", "4111111111111111")]
    [InlineData("Ada Lovelace", "")]
    [InlineData("Ada Lovelace", "   ")]
    public async Task CreateAsync_WithMissingRequiredField_ThrowsValidationException(string cardholderName, string cardNumber)
    {
        var request = new CreateCreditCardRequest(cardholderName, cardNumber, null, 1000m);

        await Assert.ThrowsAsync<ValidationException>(() => _sut.CreateAsync(request, CancellationToken.None));

        _creditCardRepository.Verify(r => r.Add(It.IsAny<CreditCard>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_WithNegativeCreditLimit_ThrowsValidationException()
    {
        var request = new CreateCreditCardRequest("Ada Lovelace", "4111111111111111", "VISA", -1m);

        var exception = await Assert.ThrowsAsync<ValidationException>(() => _sut.CreateAsync(request, CancellationToken.None));

        Assert.Contains("creditLimit", exception.Errors.Keys);
    }

    [Fact]
    public async Task GetByIdAsync_WhenCardExists_ReturnsResponse()
    {
        var creditCard = new CreditCard { Id = 1, CardholderName = "Ada", CardNumber = "4111111111111111", CreditLimit = 1000m, CreatedAt = DateTime.UtcNow };
        _creditCardRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(creditCard);

        var response = await _sut.GetByIdAsync(1, CancellationToken.None);

        Assert.Equal(1, response.Id);
    }

    [Fact]
    public async Task GetByIdAsync_WhenCardMissing_ThrowsNotFoundException()
    {
        _creditCardRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync((CreditCard?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => _sut.GetByIdAsync(999, CancellationToken.None));
    }

    [Theory]
    [InlineData(0, 0, 1, 20)]
    [InlineData(-5, -5, 1, 20)]
    [InlineData(2, 500, 2, 100)]
    [InlineData(3, 10, 3, 10)]
    public async Task GetPagedAsync_NormalizesPagingParameters(int pageNumber, int pageSize, int expectedPageNumber, int expectedPageSize)
    {
        _creditCardRepository
            .Setup(r => r.GetPagedAsync(expectedPageNumber, expectedPageSize, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        await _sut.GetPagedAsync(pageNumber, pageSize, CancellationToken.None);

        _creditCardRepository.Verify(r => r.GetPagedAsync(expectedPageNumber, expectedPageSize, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetTransactionsForCardAsync_WhenCardMissing_ThrowsNotFoundException()
    {
        _creditCardRepository.Setup(r => r.ExistsAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);

        await Assert.ThrowsAsync<NotFoundException>(() => _sut.GetTransactionsForCardAsync(1, CancellationToken.None));

        _transactionRepository.Verify(r => r.GetByCreditCardIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetTransactionsForCardAsync_WhenCardExists_ReturnsItsTransactions()
    {
        _creditCardRepository.Setup(r => r.ExistsAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _transactionRepository
            .Setup(r => r.GetByCreditCardIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync([new Transaction { Id = 1, CreditCardId = 1, Amount = 10m, Merchant = "Amazon", CreatedAt = DateTime.UtcNow }]);

        var transactions = await _sut.GetTransactionsForCardAsync(1, CancellationToken.None);

        Assert.Single(transactions);
        Assert.Equal(1, transactions[0].CreditCardId);
    }
}
