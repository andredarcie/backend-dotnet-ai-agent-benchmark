using CreditCardApi.Data.Repositories;
using CreditCardApi.DTOs;

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
        return creditCards.Select(cc => new CreditCardResponse
        {
            Id = cc.Id,
            CardholderName = cc.CardholderName,
            CardNumber = cc.CardNumber,
            Brand = cc.Brand,
            CreditLimit = cc.CreditLimit,
            CreatedAt = cc.CreatedAt
        });
    }
}
