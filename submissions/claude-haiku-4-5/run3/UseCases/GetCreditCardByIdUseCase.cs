using CreditCardApi.DTOs;
using CreditCardApi.Repositories;

namespace CreditCardApi.UseCases;

public class GetCreditCardByIdUseCase
{
    private readonly ICreditCardRepository _repository;

    public GetCreditCardByIdUseCase(ICreditCardRepository repository)
    {
        _repository = repository;
    }

    public async Task<CreditCardResponse?> ExecuteAsync(int id)
    {
        var creditCard = await _repository.GetByIdAsync(id);
        if (creditCard == null)
            return null;

        return new CreditCardResponse
        {
            Id = creditCard.Id,
            CardholderName = creditCard.CardholderName,
            CardNumber = creditCard.CardNumber,
            Brand = creditCard.Brand,
            CreditLimit = creditCard.CreditLimit,
            CreatedAt = creditCard.CreatedAt
        };
    }
}
