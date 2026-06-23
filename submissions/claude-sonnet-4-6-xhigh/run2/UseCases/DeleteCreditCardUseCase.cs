using CreditCardApi.Data.Repositories;

namespace CreditCardApi.UseCases;

public class DeleteCreditCardUseCase
{
    private readonly ICreditCardRepository _repository;

    public DeleteCreditCardUseCase(ICreditCardRepository repository)
        => _repository = repository;

    public async Task<bool> ExecuteAsync(int id)
    {
        var card = await _repository.GetByIdAsync(id);
        if (card is null)
            return false;

        await _repository.DeleteAsync(card);
        return true;
    }
}
