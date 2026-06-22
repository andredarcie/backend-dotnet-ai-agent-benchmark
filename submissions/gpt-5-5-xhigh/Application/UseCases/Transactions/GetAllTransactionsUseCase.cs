using CreditCardApi.Contracts.Transactions;
using CreditCardApi.Domain.Repositories;

namespace CreditCardApi.Application.UseCases.Transactions;

public sealed class GetAllTransactionsUseCase(ITransactionRepository repository)
{
    public async Task<IReadOnlyList<TransactionResponse>> ExecuteAsync(
        CancellationToken cancellationToken = default)
    {
        var transactions = await repository.GetAllAsync(cancellationToken);
        return transactions.Select(TransactionResponse.FromEntity).ToList();
    }
}
