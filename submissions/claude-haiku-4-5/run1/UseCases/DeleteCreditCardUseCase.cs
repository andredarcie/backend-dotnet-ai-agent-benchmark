using CreditCardApi.Data.Repositories;

namespace CreditCardApi.UseCases;

public class DeleteCreditCardUseCase
{
    private readonly ICreditCardRepository _repository;

    public DeleteCreditCardUseCase(ICreditCardRepository repository)
    {
        _repository = repository;
    }

    public async Task<bool> ExecuteAsync(int id)
    {
        var creditCard = await _repository.GetByIdAsync(id);
        if (creditCard == null)
            return false;

        await _repository.DeleteAsync(creditCard);
        await _repository.SaveChangesAsync();
        return true;
    }
}
