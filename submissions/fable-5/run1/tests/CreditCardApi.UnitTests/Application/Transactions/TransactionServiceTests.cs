using CreditCardApi.Application.Abstractions;
using CreditCardApi.Application.Exceptions;
using CreditCardApi.Application.Transactions;
using CreditCardApi.Domain.Entities;
using CreditCardApi.UnitTests.TestDoubles;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace CreditCardApi.UnitTests.Application.Transactions;

public class TransactionServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 6, 12, 0, 0, TimeSpan.Zero);

    private readonly ITransactionRepository _transactions = Substitute.For<ITransactionRepository>();
    private readonly ICreditCardRepository _cards = Substitute.For<ICreditCardRepository>();
    private readonly ITransactionEventPublisher _events = Substitute.For<ITransactionEventPublisher>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly TransactionService _service;

    public TransactionServiceTests()
    {
        // The unit-of-work double executes the transactional block inline, like the real one.
        _unitOfWork
            .ExecuteInTransactionAsync(Arg.Any<Func<CancellationToken, Task>>(), Arg.Any<CancellationToken>())
            .Returns(call => call.Arg<Func<CancellationToken, Task>>()(CancellationToken.None));

        _service = new TransactionService(
            _transactions,
            _cards,
            _events,
            _unitOfWork,
            new FixedTimeProvider(Now),
            NullLogger<TransactionService>.Instance);
    }

    [Fact]
    public async Task CreateAsync_PersistsAndStagesEventInSameTransaction()
    {
        _cards.ExistsAsync(1, Arg.Any<CancellationToken>()).Returns(true);
        Transaction? stored = null;
        _transactions.When(t => t.Add(Arg.Any<Transaction>())).Do(call => stored = call.Arg<Transaction>());

        var response = await _service.CreateAsync(
            new TransactionRequest { CreditCardId = 1, Amount = 199.90m, Merchant = "Amazon", Category = "shopping" },
            CancellationToken.None);

        Assert.NotNull(stored);
        Assert.Equal(199.90m, response.Amount);
        Assert.Equal("Amazon", response.Merchant);
        Assert.Equal("shopping", response.Category);
        Assert.Equal(Now.UtcDateTime, response.CreatedAt);

        // Both saves happen inside the explicit transaction, and the event is staged exactly once.
        await _unitOfWork.Received(1).ExecuteInTransactionAsync(
            Arg.Any<Func<CancellationToken, Task>>(),
            Arg.Any<CancellationToken>());
        await _unitOfWork.Received(2).SaveChangesAsync(Arg.Any<CancellationToken>());
        _events.Received(1).EnqueueTransactionCreated(response);
    }

    [Fact]
    public async Task CreateAsync_UnknownCreditCard_ThrowsAndStagesNothing()
    {
        _cards.ExistsAsync(999, Arg.Any<CancellationToken>()).Returns(false);

        await Assert.ThrowsAsync<BusinessRuleViolationException>(() => _service.CreateAsync(
            new TransactionRequest { CreditCardId = 999, Amount = 10m, Merchant = "Amazon" },
            CancellationToken.None));

        _transactions.DidNotReceive().Add(Arg.Any<Transaction>());
        _events.DidNotReceive().EnqueueTransactionCreated(Arg.Any<TransactionResponse>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_RoundsAmountToTwoDecimals()
    {
        _cards.ExistsAsync(1, Arg.Any<CancellationToken>()).Returns(true);

        var response = await _service.CreateAsync(
            new TransactionRequest { CreditCardId = 1, Amount = 10.005m, Merchant = "Shop" },
            CancellationToken.None);

        Assert.Equal(10.01m, response.Amount);
    }

    [Fact]
    public async Task CreateAsync_AmountThatRoundsToZero_IsRejected()
    {
        _cards.ExistsAsync(1, Arg.Any<CancellationToken>()).Returns(true);

        await Assert.ThrowsAsync<BusinessRuleViolationException>(() => _service.CreateAsync(
            new TransactionRequest { CreditCardId = 1, Amount = 0.001m, Merchant = "Shop" },
            CancellationToken.None));
    }

    [Fact]
    public async Task UpdateAsync_UnknownId_ReturnsNull()
    {
        _transactions.GetForUpdateAsync(42, Arg.Any<CancellationToken>()).Returns((Transaction?)null);

        var result = await _service.UpdateAsync(
            42,
            new TransactionRequest { CreditCardId = 1, Amount = 10m, Merchant = "Shop" },
            CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateAsync_MovingToUnknownCard_Throws()
    {
        _transactions.GetForUpdateAsync(5, Arg.Any<CancellationToken>())
            .Returns(new Transaction { Id = 5, CreditCardId = 1, Amount = 10m, Merchant = "Shop" });
        _cards.ExistsAsync(2, Arg.Any<CancellationToken>()).Returns(false);

        await Assert.ThrowsAsync<BusinessRuleViolationException>(() => _service.UpdateAsync(
            5,
            new TransactionRequest { CreditCardId = 2, Amount = 10m, Merchant = "Shop" },
            CancellationToken.None));
    }

    [Fact]
    public async Task UpdateAsync_ReplacesFieldsWithoutPublishingEvents()
    {
        _transactions.GetForUpdateAsync(5, Arg.Any<CancellationToken>())
            .Returns(new Transaction
            {
                Id = 5,
                CreditCardId = 1,
                Amount = 10m,
                Merchant = "Shop",
                CreatedAt = Now.UtcDateTime.AddDays(-1),
            });

        var result = await _service.UpdateAsync(
            5,
            new TransactionRequest { CreditCardId = 1, Amount = 25.50m, Merchant = "Bookstore", Category = "books" },
            CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(25.50m, result.Amount);
        Assert.Equal("Bookstore", result.Merchant);
        Assert.Equal("books", result.Category);

        // Events are only ever published for creations.
        _events.DidNotReceive().EnqueueTransactionCreated(Arg.Any<TransactionResponse>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task DeleteAsync_ReportsWhetherTheTransactionExisted(bool deleted)
    {
        _transactions.DeleteAsync(5, Arg.Any<CancellationToken>()).Returns(deleted);

        Assert.Equal(deleted, await _service.DeleteAsync(5, CancellationToken.None));
    }
}
