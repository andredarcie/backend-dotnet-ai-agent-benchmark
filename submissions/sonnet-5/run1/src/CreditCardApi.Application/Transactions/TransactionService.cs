using CreditCardApi.Application.Abstractions;
using CreditCardApi.Application.Common;
using CreditCardApi.Application.Exceptions;
using CreditCardApi.Domain.Entities;

namespace CreditCardApi.Application.Transactions;

public class TransactionService(
    ITransactionRepository transactionRepository,
    ICreditCardRepository creditCardRepository,
    ITransactionEventPublisher eventPublisher,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    public async Task<PagedResult<TransactionResponse>> ListAsync(PaginationQuery query, CancellationToken cancellationToken)
    {
        var (items, totalCount) = await transactionRepository.ListReadOnlyAsync(query.Page, query.PageSize, cancellationToken);
        return new PagedResult<TransactionResponse>(
            items.Select(TransactionMapping.ToResponse).ToList(), totalCount, query.Page, query.PageSize);
    }

    public async Task<TransactionResponse> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        var transaction = await transactionRepository.FindReadOnlyAsync(id, cancellationToken)
            ?? throw new NotFoundException($"Transaction {id} was not found.");
        return TransactionMapping.ToResponse(transaction);
    }

    /// <summary>
    /// Persists the transaction and stages its "created" event in the same unit of work, then publishes
    /// after the commit succeeds — no event is ever staged for a request that ultimately fails.
    /// </summary>
    public async Task<TransactionResponse> CreateAsync(TransactionRequest request, CancellationToken cancellationToken)
    {
        if (!await creditCardRepository.ExistsAsync(request.CreditCardId, cancellationToken))
        {
            throw new BusinessRuleViolationException($"Credit card {request.CreditCardId} does not exist.");
        }

        var transaction = new Transaction(
            request.CreditCardId, request.Amount, request.Merchant, request.Category, timeProvider.GetUtcNowTruncatedToMicroseconds());

        transactionRepository.Add(transaction);
        eventPublisher.Stage(transaction);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return TransactionMapping.ToResponse(transaction);
    }

    public async Task<TransactionResponse> UpdateAsync(int id, TransactionRequest request, CancellationToken cancellationToken)
    {
        var transaction = await transactionRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException($"Transaction {id} was not found.");

        if (transaction.CreditCardId != request.CreditCardId)
        {
            throw new BusinessRuleViolationException("A transaction cannot be moved to a different credit card.");
        }

        transaction.UpdateDetails(request.Amount, request.Merchant, request.Category);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return TransactionMapping.ToResponse(transaction);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken)
    {
        var transaction = await transactionRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException($"Transaction {id} was not found.");

        transactionRepository.Remove(transaction);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
