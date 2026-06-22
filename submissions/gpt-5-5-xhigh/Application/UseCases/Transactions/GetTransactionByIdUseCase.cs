using CreditCardApi.Contracts.Transactions;
using CreditCardApi.Domain.Repositories;

namespace CreditCardApi.Application.UseCases.Transactions;

public sealed class GetTransactionByIdUseCase(ITransactionRepository repository)
{
    public async Task<TransactionResponse?> ExecuteAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        var transaction = await repository.GetByIdAsync(id, cancellationToken);
        return transaction is null ? null : TransactionResponse.FromEntity(transaction);
    }
}
