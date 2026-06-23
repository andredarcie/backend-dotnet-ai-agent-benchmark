using CreditCardApi.Exceptions;
using CreditCardApi.Repositories.Interfaces;

namespace CreditCardApi.UseCases.CreditCards;

public class DeleteCreditCardUseCase
{
    private readonly ICreditCardRepository _repository;

    public DeleteCreditCardUseCase(ICreditCardRepository repository)
    {
        _repository = repository;
    }

    public async Task ExecuteAsync(int id)
    {
        var existing = await _repository.GetByIdAsync(id);
        if (existing == null)
            throw new NotFoundException($"Credit card with id {id} not found");

        await _repository.DeleteAsync(existing);
    }
}
