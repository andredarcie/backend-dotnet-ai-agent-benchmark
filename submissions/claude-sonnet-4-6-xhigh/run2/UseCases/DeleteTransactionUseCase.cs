using CreditCardApi.Data.Repositories;

namespace CreditCardApi.UseCases;

public class DeleteTransactionUseCase
{
    private readonly ITransactionRepository _repository;

    public DeleteTransactionUseCase(ITransactionRepository repository)
        => _repository = repository;

    public async Task<bool> ExecuteAsync(int id)
    {
        var transaction = await _repository.GetByIdAsync(id);
        if (transaction is null)
            return false;

        await _repository.DeleteAsync(transaction);
        return true;
    }
}
