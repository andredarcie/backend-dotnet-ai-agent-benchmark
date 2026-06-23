using CreditCardApi.Application.Common;
using CreditCardApi.Contracts.Transactions;
using CreditCardApi.Domain.Repositories;

namespace CreditCardApi.Application.UseCases.Transactions;

public sealed class UpdateTransactionUseCase(
    ITransactionRepository transactionRepository,
    ICreditCardRepository creditCardRepository)
{
    public async Task<UseCaseResult<TransactionResponse>> ExecuteAsync(
        int id,
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

        var transaction = await transactionRepository.GetByIdAsync(id, cancellationToken);
        if (transaction is null)
            return UseCaseResult<TransactionResponse>.NotFound();

        transaction.CreditCardId = request.CreditCardId;
        transaction.Amount = request.Amount;
        transaction.Merchant = request.Merchant!.Trim();
        transaction.Category = request.Category;

        await transactionRepository.UpdateAsync(transaction, cancellationToken);
        return UseCaseResult<TransactionResponse>.Success(TransactionResponse.FromEntity(transaction));
    }
}
