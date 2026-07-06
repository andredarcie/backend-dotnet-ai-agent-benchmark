using CreditCardApi.Application.Abstractions;
using CreditCardApi.Application.Exceptions;
using CreditCardApi.Application.Transactions;
using CreditCardApi.Domain.Entities;
using CreditCardApi.UnitTests.TestDoubles;
using NSubstitute;

namespace CreditCardApi.UnitTests.Application.Transactions;

public class TransactionServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

    private readonly ITransactionRepository _transactionRepository = Substitute.For<ITransactionRepository>();
    private readonly ICreditCardRepository _creditCardRepository = Substitute.For<ICreditCardRepository>();
    private readonly ITransactionEventPublisher _eventPublisher = Substitute.For<ITransactionEventPublisher>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly TransactionService _sut;

    public TransactionServiceTests()
    {
        _sut = new TransactionService(
            _transactionRepository, _creditCardRepository, _eventPublisher, _unitOfWork, new FixedTimeProvider(Now));
    }

    [Fact]
    public async Task CreateAsync_ThrowsBusinessRuleViolation_WhenCreditCardDoesNotExist()
    {
        _creditCardRepository.ExistsAsync(1, Arg.Any<CancellationToken>()).Returns(false);
        var request = new TransactionRequest { CreditCardId = 1, Amount = 10m, Merchant = "Amazon" };

        await Assert.ThrowsAsync<BusinessRuleViolationException>(() => _sut.CreateAsync(request, CancellationToken.None));

        _transactionRepository.DidNotReceive().Add(Arg.Any<Transaction>());
        _eventPublisher.DidNotReceive().Stage(Arg.Any<Transaction>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_PersistsAndStagesTheEvent_WhenCreditCardExists()
    {
        _creditCardRepository.ExistsAsync(1, Arg.Any<CancellationToken>()).Returns(true);
        var request = new TransactionRequest { CreditCardId = 1, Amount = 199.90m, Merchant = "Amazon", Category = "shopping" };

        var response = await _sut.CreateAsync(request, CancellationToken.None);

        Assert.Equal(199.90m, response.Amount);
        Assert.Equal(Now.UtcDateTime, response.CreatedAt);
        _transactionRepository.Received(1).Add(Arg.Is<Transaction>(t => t.Amount == 199.90m && t.Merchant == "Amazon"));
        _eventPublisher.Received(1).Stage(Arg.Any<Transaction>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetByIdAsync_ThrowsNotFound_WhenMissing()
    {
        _transactionRepository.FindReadOnlyAsync(1, Arg.Any<CancellationToken>()).Returns((Transaction?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => _sut.GetByIdAsync(1, CancellationToken.None));
    }

    [Fact]
    public async Task UpdateAsync_ThrowsNotFound_WhenMissing()
    {
        _transactionRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns((Transaction?)null);
        var request = new TransactionRequest { CreditCardId = 1, Amount = 10m, Merchant = "Amazon" };

        await Assert.ThrowsAsync<NotFoundException>(() => _sut.UpdateAsync(1, request, CancellationToken.None));
    }

    [Fact]
    public async Task UpdateAsync_ThrowsBusinessRuleViolation_WhenMovingToADifferentCard()
    {
        var transaction = new Transaction(1, 10m, "Amazon", null, Now.UtcDateTime);
        _transactionRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(transaction);
        var request = new TransactionRequest { CreditCardId = 2, Amount = 10m, Merchant = "Amazon" };

        await Assert.ThrowsAsync<BusinessRuleViolationException>(() => _sut.UpdateAsync(1, request, CancellationToken.None));
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateAsync_AppliesNewValues_WhenCardIdUnchanged()
    {
        var transaction = new Transaction(1, 10m, "Amazon", null, Now.UtcDateTime);
        _transactionRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(transaction);
        var request = new TransactionRequest { CreditCardId = 1, Amount = 25m, Merchant = "eBay", Category = "electronics" };

        var response = await _sut.UpdateAsync(1, request, CancellationToken.None);

        Assert.Equal(25m, response.Amount);
        Assert.Equal("eBay", response.Merchant);
        Assert.Equal("electronics", response.Category);
    }

    [Fact]
    public async Task DeleteAsync_ThrowsNotFound_WhenMissing()
    {
        _transactionRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns((Transaction?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => _sut.DeleteAsync(1, CancellationToken.None));
    }

    [Fact]
    public async Task DeleteAsync_RemovesAndSaves_WhenFound()
    {
        var transaction = new Transaction(1, 10m, "Amazon", null, Now.UtcDateTime);
        _transactionRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(transaction);

        await _sut.DeleteAsync(1, CancellationToken.None);

        _transactionRepository.Received(1).Remove(transaction);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
