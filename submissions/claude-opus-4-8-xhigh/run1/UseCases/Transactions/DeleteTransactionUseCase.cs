using CreditCardApi.Application;
using CreditCardApi.Repositories;

namespace CreditCardApi.UseCases.Transactions;

public class DeleteTransactionUseCase
{
    private readonly ITransactionRepository _repository;

    public DeleteTransactionUseCase(ITransactionRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result> ExecuteAsync(int id, CancellationToken ct = default)
    {
        var transaction = await _repository.GetByIdAsync(id, ct);
        if (transaction is null)
        {
            return Result.NotFound();
        }

        await _repository.DeleteAsync(transaction, ct);
        return Result.Success();
    }
}
