using CreditCardApi.Application.Abstractions;
using CreditCardApi.Application.Common;
using CreditCardApi.Application.CreditCards;
using CreditCardApi.Domain.Entities;
using CreditCardApi.UnitTests.TestDoubles;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace CreditCardApi.UnitTests.Application.CreditCards;

public class CreditCardServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 6, 12, 0, 0, TimeSpan.Zero);

    private readonly ICreditCardRepository _cards = Substitute.For<ICreditCardRepository>();
    private readonly ITransactionRepository _transactions = Substitute.For<ITransactionRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly CreditCardService _service;

    public CreditCardServiceTests()
    {
        _service = new CreditCardService(
            _cards,
            _transactions,
            _unitOfWork,
            new FixedTimeProvider(Now),
            NullLogger<CreditCardService>.Instance);
    }

    [Fact]
    public async Task CreateAsync_StoresOnlyLast4AndReturnsMaskedNumber()
    {
        CreditCard? stored = null;
        _cards.When(c => c.Add(Arg.Any<CreditCard>())).Do(call => stored = call.Arg<CreditCard>());

        var response = await _service.CreateAsync(
            new CreditCardRequest
            {
                CardholderName = "Ada Lovelace",
                CardNumber = "4111 1111 1111 1111",
                Brand = "VISA",
                CreditLimit = 5000m,
            },
            CancellationToken.None);

        Assert.NotNull(stored);
        Assert.Equal("1111", stored.CardNumberLast4);
        Assert.Equal("**** **** **** 1111", response.CardNumber);
        Assert.Equal(Now.UtcDateTime, response.CreatedAt);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_RoundsCreditLimitToTwoDecimals()
    {
        var response = await _service.CreateAsync(
            new CreditCardRequest
            {
                CardholderName = "Ada Lovelace",
                CardNumber = "4111111111111111",
                CreditLimit = 1000.005m,
            },
            CancellationToken.None);

        Assert.Equal(1000.01m, response.CreditLimit);
    }

    [Fact]
    public async Task GetAsync_UnknownId_ReturnsNull()
    {
        _cards.GetAsync(42, Arg.Any<CancellationToken>()).Returns((CreditCard?)null);

        Assert.Null(await _service.GetAsync(42, CancellationToken.None));
    }

    [Fact]
    public async Task UpdateAsync_UnknownId_ReturnsNullWithoutSaving()
    {
        _cards.GetForUpdateAsync(42, Arg.Any<CancellationToken>()).Returns((CreditCard?)null);

        var result = await _service.UpdateAsync(
            42,
            new CreditCardRequest { CardholderName = "X", CardNumber = "4111111111111111", CreditLimit = 1m },
            CancellationToken.None);

        Assert.Null(result);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateAsync_ReplacesFieldsAndReturnsMaskedNumber()
    {
        var existing = new CreditCard
        {
            Id = 7,
            CardholderName = "Old Name",
            CardNumberLast4 = "0000",
            Brand = "VISA",
            CreditLimit = 100m,
            CreatedAt = Now.UtcDateTime.AddDays(-1),
        };
        _cards.GetForUpdateAsync(7, Arg.Any<CancellationToken>()).Returns(existing);

        var result = await _service.UpdateAsync(
            7,
            new CreditCardRequest
            {
                CardholderName = "New Name",
                CardNumber = "5500-0000-0000-0004",
                Brand = "  MASTERCARD  ",
                CreditLimit = 250m,
            },
            CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("New Name", result.CardholderName);
        Assert.Equal("**** **** **** 0004", result.CardNumber);
        Assert.Equal("MASTERCARD", result.Brand);
        Assert.Equal(250m, result.CreditLimit);
        Assert.Equal(existing.CreatedAt, result.CreatedAt);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task DeleteAsync_ReportsWhetherTheCardExisted(bool deleted)
    {
        _cards.DeleteAsync(7, Arg.Any<CancellationToken>()).Returns(deleted);

        Assert.Equal(deleted, await _service.DeleteAsync(7, CancellationToken.None));
    }

    [Fact]
    public async Task GetTransactionsAsync_UnknownCard_ReturnsNull()
    {
        _cards.ExistsAsync(42, Arg.Any<CancellationToken>()).Returns(false);

        Assert.Null(await _service.GetTransactionsAsync(42, new PaginationQuery(), CancellationToken.None));
    }

    [Fact]
    public async Task GetTransactionsAsync_ExistingCard_ReturnsMappedPage()
    {
        _cards.ExistsAsync(7, Arg.Any<CancellationToken>()).Returns(true);
        _transactions.GetPageForCardAsync(7, Arg.Any<PaginationQuery>(), Arg.Any<CancellationToken>())
            .Returns(new PagedResult<Transaction>(
                [new Transaction { Id = 1, CreditCardId = 7, Amount = 10m, Merchant = "Shop", CreatedAt = Now.UtcDateTime }],
                1,
                1,
                20));

        var page = await _service.GetTransactionsAsync(7, new PaginationQuery(), CancellationToken.None);

        Assert.NotNull(page);
        var item = Assert.Single(page.Items);
        Assert.Equal(1, item.Id);
        Assert.Equal(7, item.CreditCardId);
        Assert.Equal(1, page.TotalCount);
    }
}
