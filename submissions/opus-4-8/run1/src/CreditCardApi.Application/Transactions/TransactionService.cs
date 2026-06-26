using System.Globalization;
using CreditCardApi.Application.Abstractions;
using CreditCardApi.Application.Common;
using CreditCardApi.Application.Exceptions;
using CreditCardApi.Application.Mapping;
using CreditCardApi.Domain.Entities;
using CreditCardApi.Domain.Exceptions;

namespace CreditCardApi.Application.Transactions;

/// <summary>Use cases for transactions, including the reliable publish of the created event.</summary>
public sealed class TransactionService
{
    /// <summary>Kafka topic that created transactions are published to.</summary>
    public const string Topic = "transactions";

    /// <summary>Logical event type recorded on the outbox row.</summary>
    public const string TransactionCreatedEvent = "transaction.created";

    private readonly ITransactionRepository _transactions;
    private readonly ICreditCardRepository _cards;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IIntegrationEventPublisher _events;
    private readonly IClock _clock;

    /// <summary>Creates the service with its collaborators.</summary>
    public TransactionService(
        ITransactionRepository transactions,
        ICreditCardRepository cards,
        IUnitOfWork unitOfWork,
        IIntegrationEventPublisher events,
        IClock clock)
    {
        _transactions = transactions;
        _cards = cards;
        _unitOfWork = unitOfWork;
        _events = events;
        _clock = clock;
    }

    /// <summary>Returns a page of transactions.</summary>
    public async Task<PagedResult<TransactionResponse>> ListAsync(PageRequest page, CancellationToken cancellationToken)
    {
        var total = await _transactions.CountAsync(cancellationToken);
        var items = await _transactions.ListAsync(page.Skip, page.PageSize, cancellationToken);
        var responses = items.Select(t => t.ToResponse()).ToList();
        return new PagedResult<TransactionResponse>(responses, page.Page, page.PageSize, total);
    }

    /// <summary>Returns a single transaction, or throws <see cref="NotFoundException"/>.</summary>
    public async Task<TransactionResponse> GetAsync(int id, CancellationToken cancellationToken)
    {
        var transaction = await _transactions.GetByIdAsync(id, cancellationToken)
                          ?? throw new NotFoundException(nameof(Transaction), id);
        return transaction.ToResponse();
    }

    /// <summary>
    /// Creates a transaction. The row and its outbox event are committed in one transaction, so the
    /// event is published exactly when (and only when) the transaction is durably stored.
    /// </summary>
    public async Task<TransactionResponse> CreateAsync(CreateTransactionRequest request, CancellationToken cancellationToken)
    {
        if (!await _cards.ExistsAsync(request.CreditCardId, cancellationToken))
        {
            // FK does not exist -> 400, per the contract.
            throw new DomainValidationException(
                nameof(request.CreditCardId),
                $"Credit card with id '{request.CreditCardId}' does not exist.");
        }

        TransactionResponse? response = null;
        await _unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            var transaction = Transaction.Create(
                request.CreditCardId,
                request.Amount,
                request.Merchant,
                request.Category,
                _clock.UtcNow);

            _transactions.Add(transaction);
            await _unitOfWork.SaveChangesAsync(token); // assigns the generated id

            response = transaction.ToResponse();
            _events.Enqueue(
                Topic,
                transaction.Id.ToString(CultureInfo.InvariantCulture),
                TransactionCreatedEvent,
                response);
            await _unitOfWork.SaveChangesAsync(token); // outbox row in the same transaction
        }, cancellationToken);

        return response!;
    }

    /// <summary>Updates an existing transaction, or throws <see cref="NotFoundException"/>.</summary>
    public async Task UpdateAsync(int id, UpdateTransactionRequest request, CancellationToken cancellationToken)
    {
        var transaction = await _transactions.GetByIdAsync(id, cancellationToken)
                          ?? throw new NotFoundException(nameof(Transaction), id);
        transaction.Update(request.Amount, request.Merchant, request.Category);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    /// <summary>Deletes a transaction, or throws <see cref="NotFoundException"/>.</summary>
    public async Task DeleteAsync(int id, CancellationToken cancellationToken)
    {
        var transaction = await _transactions.GetByIdAsync(id, cancellationToken)
                          ?? throw new NotFoundException(nameof(Transaction), id);
        _transactions.Remove(transaction);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
