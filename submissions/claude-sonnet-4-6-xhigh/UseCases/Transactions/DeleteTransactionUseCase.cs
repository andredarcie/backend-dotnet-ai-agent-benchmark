using CreditCardApi.Exceptions;
using CreditCardApi.Repositories.Interfaces;

namespace CreditCardApi.UseCases.Transactions;

public class DeleteTransactionUseCase
{
    private readonly ITransactionRepository _repository;

    public DeleteTransactionUseCase(ITransactionRepository repository)
    {
        _repository = repository;
    }

    public async Task ExecuteAsync(int id)
    {
        var existing = await _repository.GetByIdAsync(id);
        if (existing == null)
            throw new NotFoundException($"Transaction with id {id} not found");

        await _repository.DeleteAsync(existing);
    }
}
