using CreditCardApi.Application.Common;
using CreditCardApi.Application.Messaging;
using CreditCardApi.Contracts.Transactions;
using CreditCardApi.Domain.Entities;
using CreditCardApi.Domain.Repositories;

namespace CreditCardApi.Application.UseCases.Transactions;

public sealed class CreateTransactionUseCase(
    ITransactionRepository transactionRepository,
    ICreditCardRepository creditCardRepository,
    ITransactionEventPublisher eventPublisher)
{
    public async Task<UseCaseResult<TransactionResponse>> ExecuteAsync(
        TransactionRequest request,
        CancellationToken cancellationToken = default)
    {
        var errors = Validation.Validate(request).ToList();

        if (request.CreditCardId > 0 &&
            !await creditCardRepository.ExistsAsync(request.CreditCardId, cancellationToken))
        {
            errors.Add("creditCardId must reference an existing credit card.");
        }

        if (errors.Count > 0)
            return UseCaseResult<TransactionResponse>.Invalid(errors.ToArray());

        var transaction = new Transaction
        {
            CreditCardId = request.CreditCardId,
            Amount = request.Amount,
            Merchant = request.Merchant!.Trim(),
            Category = request.Category,
            CreatedAt = DateTime.UtcNow
        };

        await transactionRepository.AddAsync(transaction, cancellationToken);

        var response = TransactionResponse.FromEntity(transaction);
        await eventPublisher.PublishCreatedAsync(response, CancellationToken.None);

        return UseCaseResult<TransactionResponse>.Success(response);
    }
}
