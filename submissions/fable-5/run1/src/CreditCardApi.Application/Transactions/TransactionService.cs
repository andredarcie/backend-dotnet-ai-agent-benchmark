using CreditCardApi.Application.Abstractions;
using CreditCardApi.Application.Common;
using CreditCardApi.Application.Exceptions;
using CreditCardApi.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace CreditCardApi.Application.Transactions;

/// <summary>Use cases for managing transactions.</summary>
/// <remarks>
/// Request DTOs are assumed to have passed model validation at the API boundary; this service
/// enforces the rules that need data access (the referenced card must exist) and normalizes
/// amounts to two decimal places.
/// </remarks>
public sealed class TransactionService
{
    private readonly ITransactionRepository _transactions;
    private readonly ICreditCardRepository _cards;
    private readonly ITransactionEventPublisher _events;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _clock;
    private readonly ILogger<TransactionService> _logger;

    /// <summary>Creates the service with its collaborators.</summary>
    public TransactionService(
        ITransactionRepository transactions,
        ICreditCardRepository cards,
        ITransactionEventPublisher events,
        IUnitOfWork unitOfWork,
        TimeProvider clock,
        ILogger<TransactionService> logger)
    {
        _transactions = transactions;
        _cards = cards;
        _events = events;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _logger = logger;
    }

    /// <summary>Returns one page of transactions.</summary>
    public async Task<PagedResult<TransactionResponse>> GetPageAsync(PaginationQuery page, CancellationToken cancellationToken)
    {
        var result = await _transactions.GetPageAsync(page, cancellationToken);
        return result.Map(transaction => transaction.ToResponse());
    }

    /// <summary>Returns a single transaction, or <see langword="null"/> if it does not exist.</summary>
    public async Task<TransactionResponse?> GetAsync(int id, CancellationToken cancellationToken)
    {
        var transaction = await _transactions.GetAsync(id, cancellationToken);
        return transaction?.ToResponse();
    }

    /// <summary>
    /// Creates a transaction and atomically stages its "created" event for delivery to the broker
    /// (transactional outbox). The event is only ever published for a successfully persisted row.
    /// </summary>
    /// <exception cref="BusinessRuleViolationException">The referenced credit card does not exist.</exception>
    public async Task<TransactionResponse> CreateAsync(TransactionRequest request, CancellationToken cancellationToken)
    {
        var creditCardId = request.CreditCardId!.Value;
        await EnsureCardExistsAsync(creditCardId, cancellationToken);

        var transaction = new Transaction
        {
            CreditCardId = creditCardId,
            Amount = NormalizeAmount(request.Amount!.Value),
            Merchant = request.Merchant!.Trim(),
            Category = NormalizeCategory(request.Category),
            CreatedAt = _clock.GetUtcNowForStorage(),
        };

        TransactionResponse? response = null;

        // The row and its outbox message must commit together, but the message payload needs the
        // database-generated id — hence two saves inside one explicit transaction.
        await _unitOfWork.ExecuteInTransactionAsync(
            async ct =>
            {
                _transactions.Add(transaction);
                await _unitOfWork.SaveChangesAsync(ct);

                response = transaction.ToResponse();
                _events.EnqueueTransactionCreated(response);
                await _unitOfWork.SaveChangesAsync(ct);
            },
            cancellationToken);

        _logger.LogInformation(
            "Created transaction {TransactionId} for credit card {CreditCardId}",
            transaction.Id,
            transaction.CreditCardId);

        return response!;
    }

    /// <summary>Replaces a transaction's data. Returns <see langword="null"/> if it does not exist.</summary>
    /// <exception cref="BusinessRuleViolationException">The referenced credit card does not exist.</exception>
    public async Task<TransactionResponse?> UpdateAsync(int id, TransactionRequest request, CancellationToken cancellationToken)
    {
        var transaction = await _transactions.GetForUpdateAsync(id, cancellationToken);
        if (transaction is null)
        {
            return null;
        }

        var creditCardId = request.CreditCardId!.Value;
        if (creditCardId != transaction.CreditCardId)
        {
            await EnsureCardExistsAsync(creditCardId, cancellationToken);
        }

        transaction.CreditCardId = creditCardId;
        transaction.Amount = NormalizeAmount(request.Amount!.Value);
        transaction.Merchant = request.Merchant!.Trim();
        transaction.Category = NormalizeCategory(request.Category);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Updated transaction {TransactionId}", transaction.Id);
        return transaction.ToResponse();
    }

    /// <summary>Deletes a transaction. Returns <see langword="false"/> if it does not exist.</summary>
    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken)
    {
        var deleted = await _transactions.DeleteAsync(id, cancellationToken);
        if (deleted)
        {
            _logger.LogInformation("Deleted transaction {TransactionId}", id);
        }

        return deleted;
    }

    private async Task EnsureCardExistsAsync(int creditCardId, CancellationToken cancellationToken)
    {
        if (!await _cards.ExistsAsync(creditCardId, cancellationToken))
        {
            throw new BusinessRuleViolationException($"Credit card '{creditCardId}' does not exist.");
        }
    }

    private static decimal NormalizeAmount(decimal amount)
    {
        var normalized = decimal.Round(amount, 2, MidpointRounding.AwayFromZero);
        if (normalized <= 0m)
        {
            throw new BusinessRuleViolationException(
                "The amount must be greater than zero after rounding to two decimal places.");
        }

        return normalized;
    }

    private static string? NormalizeCategory(string? category) =>
        string.IsNullOrWhiteSpace(category) ? null : category.Trim();
}
