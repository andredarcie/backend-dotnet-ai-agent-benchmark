using CreditCardApi.Exceptions;
using CreditCardApi.Models;
using CreditCardApi.Repositories.Interfaces;

namespace CreditCardApi.UseCases.CreditCards;

public class GetCreditCardByIdUseCase
{
    private readonly ICreditCardRepository _repository;

    public GetCreditCardByIdUseCase(ICreditCardRepository repository)
    {
        _repository = repository;
    }

    public async Task<CreditCard> ExecuteAsync(int id)
    {
        var card = await _repository.GetByIdAsync(id);
        if (card == null)
            throw new NotFoundException($"Credit card with id {id} not found");
        return card;
    }
}
