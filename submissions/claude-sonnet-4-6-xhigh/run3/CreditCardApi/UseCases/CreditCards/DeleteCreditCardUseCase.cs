using CreditCardApi.Repositories;

namespace CreditCardApi.UseCases.CreditCards;

public class DeleteCreditCardUseCase
{
    private readonly ICreditCardRepository _repository;

    public DeleteCreditCardUseCase(ICreditCardRepository repository)
    {
        _repository = repository;
    }

    public async Task<bool> ExecuteAsync(int id)
    {
        var card = await _repository.GetByIdAsync(id);
        if (card == null)
            return false;

        _repository.Delete(card);
        await _repository.SaveChangesAsync();
        return true;
    }
}
