using CreditCardApi.Application.Common;
using CreditCardApi.Application.Common.Exceptions;
using CreditCardApi.Application.CreditCards;
using CreditCardApi.Application.Transactions.Dtos;
using CreditCardApi.Domain.Entities;

namespace CreditCardApi.Application.Transactions;

public sealed class TransactionService(
    ITransactionRepository transactionRepository,
    ICreditCardRepository creditCardRepository,
    ITransactionEventPublisher eventPublisher,
    TimeProvider timeProvider) : ITransactionService
{
    public async Task<TransactionResponse> CreateAsync(CreateTransactionRequest request, CancellationToken cancellationToken)
    {
        var errors = new Dictionary<string, string[]>();

        if (request.Amount <= 0)
            errors["amount"] = ["Amount must be greater than 0."];

        if (string.IsNullOrWhiteSpace(request.Merchant))
            errors["merchant"] = ["Merchant is required."];

        if (!await creditCardRepository.ExistsAsync(request.CreditCardId, cancellationToken))
            errors["creditCardId"] = ["Credit card does not exist."];

        if (errors.Count > 0)
            throw new ValidationException(errors);

        var transaction = new Transaction
        {
            CreditCardId = request.CreditCardId,
            Amount = request.Amount,
            Merchant = request.Merchant,
            Category = request.Category,
            CreatedAt = timeProvider.GetUtcNow().UtcDateTime.TruncateToMicroseconds(),
        };

        transactionRepository.Add(transaction);
        await transactionRepository.SaveChangesAsync(cancellationToken);

        var response = ToResponse(transaction);

        await eventPublisher.PublishCreatedAsync(response, cancellationToken);

        return response;
    }

    public async Task<TransactionResponse> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        var transaction = await transactionRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException($"Transaction {id} was not found.");

        return ToResponse(transaction);
    }

    public async Task<IReadOnlyList<TransactionResponse>> GetPagedAsync(int pageNumber, int pageSize, CancellationToken cancellationToken)
    {
        var (normalizedPageNumber, normalizedPageSize) = Pagination.Normalize(pageNumber, pageSize);
        var transactions = await transactionRepository.GetPagedAsync(normalizedPageNumber, normalizedPageSize, cancellationToken);
        return [.. transactions.Select(ToResponse)];
    }

    internal static TransactionResponse ToResponse(Transaction transaction) => new(
        transaction.Id,
        transaction.CreditCardId,
        transaction.Amount,
        transaction.Merchant,
        transaction.Category,
        transaction.CreatedAt);
}
