using CreditCardApi.Application.Abstractions;
using CreditCardApi.Application.Common;
using CreditCardApi.Application.CreditCards;
using CreditCardApi.Application.Exceptions;
using CreditCardApi.Domain.Entities;
using CreditCardApi.UnitTests.TestDoubles;
using NSubstitute;

namespace CreditCardApi.UnitTests.Application.CreditCards;

public class CreditCardServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

    private readonly ICreditCardRepository _creditCardRepository = Substitute.For<ICreditCardRepository>();
    private readonly ITransactionRepository _transactionRepository = Substitute.For<ITransactionRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly CreditCardService _sut;

    public CreditCardServiceTests()
    {
        _sut = new CreditCardService(_creditCardRepository, _transactionRepository, _unitOfWork, new FixedTimeProvider(Now));
    }

    [Fact]
    public async Task CreateAsync_PersistsCardWithTruncatedCardNumberAndReturnsMaskedResponse()
    {
        var request = new CreditCardRequest
        {
            CardholderName = "Ada Lovelace",
            CardNumber = "4111111111111111",
            Brand = "VISA",
            CreditLimit = 5000m,
        };

        var response = await _sut.CreateAsync(request, CancellationToken.None);

        Assert.Equal("**** **** **** 1111", response.CardNumber);
        Assert.Equal(Now.UtcDateTime, response.CreatedAt);
        _creditCardRepository.Received(1).Add(Arg.Is<CreditCard>(c => c.CardNumberLast4 == "1111"));
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetByIdAsync_ThrowsNotFound_WhenCardDoesNotExist()
    {
        _creditCardRepository.FindReadOnlyAsync(1, Arg.Any<CancellationToken>()).Returns((CreditCard?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => _sut.GetByIdAsync(1, CancellationToken.None));
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsMappedResponse_WhenCardExists()
    {
        var card = new CreditCard("Ada Lovelace", "1111", "VISA", 5000m, Now.UtcDateTime);
        _creditCardRepository.FindReadOnlyAsync(1, Arg.Any<CancellationToken>()).Returns(card);

        var response = await _sut.GetByIdAsync(1, CancellationToken.None);

        Assert.Equal("Ada Lovelace", response.CardholderName);
    }

    [Fact]
    public async Task UpdateAsync_ThrowsNotFound_WhenCardDoesNotExist()
    {
        _creditCardRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns((CreditCard?)null);
        var request = new CreditCardRequest { CardholderName = "x", CardNumber = "4111111111111111", CreditLimit = 1m };

        await Assert.ThrowsAsync<NotFoundException>(() => _sut.UpdateAsync(1, request, CancellationToken.None));
    }

    [Fact]
    public async Task UpdateAsync_AppliesNewValuesAndSaves()
    {
        var card = new CreditCard("Ada Lovelace", "1111", "VISA", 5000m, Now.UtcDateTime);
        _creditCardRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(card);
        var request = new CreditCardRequest { CardholderName = "Ada L.", CardNumber = "5500005555555559", Brand = "MASTERCARD", CreditLimit = 7500m };

        var response = await _sut.UpdateAsync(1, request, CancellationToken.None);

        Assert.Equal("Ada L.", response.CardholderName);
        Assert.Equal("**** **** **** 5559", response.CardNumber);
        Assert.Equal("MASTERCARD", response.Brand);
        Assert.Equal(7500m, response.CreditLimit);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteAsync_ThrowsNotFound_WhenCardDoesNotExist()
    {
        _creditCardRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns((CreditCard?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => _sut.DeleteAsync(1, CancellationToken.None));
    }

    [Fact]
    public async Task DeleteAsync_RemovesCardAndSaves_WhenCardExists()
    {
        var card = new CreditCard("Ada Lovelace", "1111", "VISA", 5000m, Now.UtcDateTime);
        _creditCardRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(card);

        await _sut.DeleteAsync(1, CancellationToken.None);

        _creditCardRepository.Received(1).Remove(card);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ListTransactionsAsync_ThrowsNotFound_WhenCardDoesNotExist()
    {
        _creditCardRepository.ExistsAsync(1, Arg.Any<CancellationToken>()).Returns(false);

        await Assert.ThrowsAsync<NotFoundException>(
            () => _sut.ListTransactionsAsync(1, new PaginationQuery(), CancellationToken.None));
    }

    [Fact]
    public async Task ListTransactionsAsync_ReturnsPagedTransactions_WhenCardExists()
    {
        _creditCardRepository.ExistsAsync(1, Arg.Any<CancellationToken>()).Returns(true);
        var transaction = new Transaction(1, 10m, "Amazon", null, Now.UtcDateTime);
        _transactionRepository
            .ListByCreditCardReadOnlyAsync(1, 1, 20, Arg.Any<CancellationToken>())
            .Returns((new[] { transaction }, 1));

        var result = await _sut.ListTransactionsAsync(1, new PaginationQuery(), CancellationToken.None);

        Assert.Single(result.Items);
        Assert.Equal(1, result.TotalCount);
    }
}
