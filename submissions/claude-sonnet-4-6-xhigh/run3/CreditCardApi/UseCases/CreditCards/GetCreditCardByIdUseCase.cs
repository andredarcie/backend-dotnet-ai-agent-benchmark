using CreditCardApi.Models;
using CreditCardApi.Repositories;

namespace CreditCardApi.UseCases.CreditCards;

public class GetCreditCardByIdUseCase
{
    private readonly ICreditCardRepository _repository;

    public GetCreditCardByIdUseCase(ICreditCardRepository repository)
    {
        _repository = repository;
    }

    public async Task<CreditCard?> ExecuteAsync(int id)
        => await _repository.GetByIdAsync(id);
}
