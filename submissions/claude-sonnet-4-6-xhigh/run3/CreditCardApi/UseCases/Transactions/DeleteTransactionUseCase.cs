using CreditCardApi.Repositories;

namespace CreditCardApi.UseCases.Transactions;

public class DeleteTransactionUseCase
{
    private readonly ITransactionRepository _repository;

    public DeleteTransactionUseCase(ITransactionRepository repository)
    {
        _repository = repository;
    }

    public async Task<bool> ExecuteAsync(int id)
    {
        var transaction = await _repository.GetByIdAsync(id);
        if (transaction == null)
            return false;

        _repository.Delete(transaction);
        await _repository.SaveChangesAsync();
        return true;
    }
}
