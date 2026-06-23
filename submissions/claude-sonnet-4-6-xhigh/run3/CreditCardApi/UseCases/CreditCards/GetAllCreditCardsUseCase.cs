using CreditCardApi.Models;
using CreditCardApi.Repositories;

namespace CreditCardApi.UseCases.CreditCards;

public class GetAllCreditCardsUseCase
{
    private readonly ICreditCardRepository _repository;

    public GetAllCreditCardsUseCase(ICreditCardRepository repository)
    {
        _repository = repository;
    }

    public async Task<IEnumerable<CreditCard>> ExecuteAsync()
        => await _repository.GetAllAsync();
}
