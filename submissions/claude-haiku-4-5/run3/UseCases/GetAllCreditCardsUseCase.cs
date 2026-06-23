using CreditCardApi.DTOs;
using CreditCardApi.Repositories;

namespace CreditCardApi.UseCases;

public class GetAllCreditCardsUseCase
{
    private readonly ICreditCardRepository _repository;

    public GetAllCreditCardsUseCase(ICreditCardRepository repository)
    {
        _repository = repository;
    }

    public async Task<IEnumerable<CreditCardResponse>> ExecuteAsync()
    {
        var creditCards = await _repository.GetAllAsync();
        return creditCards.Select(c => new CreditCardResponse
        {
            Id = c.Id,
            CardholderName = c.CardholderName,
            CardNumber = c.CardNumber,
            Brand = c.Brand,
            CreditLimit = c.CreditLimit,
            CreatedAt = c.CreatedAt
        });
    }
}
