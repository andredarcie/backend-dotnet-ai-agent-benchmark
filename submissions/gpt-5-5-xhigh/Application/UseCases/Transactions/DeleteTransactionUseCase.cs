using CreditCardApi.Application.Common;
using CreditCardApi.Domain.Repositories;

namespace CreditCardApi.Application.UseCases.Transactions;

public sealed class DeleteTransactionUseCase(ITransactionRepository repository)
{
    public async Task<UseCaseResult<bool>> ExecuteAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        var transaction = await repository.GetByIdAsync(id, cancellationToken);
        if (transaction is null)
            return UseCaseResult<bool>.NotFound();

        await repository.DeleteAsync(transaction, cancellationToken);
        return UseCaseResult<bool>.Success(true);
    }
}
