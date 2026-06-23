using CreditCardApi.Application.Common;
using CreditCardApi.Contracts.Transactions;
using CreditCardApi.Domain.Repositories;

namespace CreditCardApi.Application.UseCases.CreditCards;

public sealed class GetCreditCardTransactionsUseCase(
    ICreditCardRepository creditCardRepository,
    ITransactionRepository transactionRepository)
{
    public async Task<UseCaseResult<IReadOnlyList<TransactionResponse>>> ExecuteAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        if (!await creditCardRepository.ExistsAsync(id, cancellationToken))
            return UseCaseResult<IReadOnlyList<TransactionResponse>>.NotFound();

        var transactions = await transactionRepository.GetByCreditCardIdAsync(id, cancellationToken);
        var response = transactions.Select(TransactionResponse.FromEntity).ToList();
        return UseCaseResult<IReadOnlyList<TransactionResponse>>.Success(response);
    }
}
