using CreditCardApi.DTOs;
using CreditCardApi.Repositories;

namespace CreditCardApi.UseCases;

public class UpdateCreditCardUseCase
{
    private readonly ICreditCardRepository _repository;

    public UpdateCreditCardUseCase(ICreditCardRepository repository)
    {
        _repository = repository;
    }

    public async Task<CreditCardResponse?> ExecuteAsync(int id, UpdateCreditCardRequest request)
    {
        var creditCard = await _repository.GetByIdAsync(id);
        if (creditCard == null)
            return null;

        if (!string.IsNullOrWhiteSpace(request.CardholderName))
            creditCard.CardholderName = request.CardholderName;
        if (!string.IsNullOrWhiteSpace(request.CardNumber))
            creditCard.CardNumber = request.CardNumber;
        if (!string.IsNullOrWhiteSpace(request.Brand))
            creditCard.Brand = request.Brand;
        if (request.CreditLimit.HasValue)
            creditCard.CreditLimit = request.CreditLimit.Value;

        var updated = await _repository.UpdateAsync(creditCard);

        return new CreditCardResponse
        {
            Id = updated.Id,
            CardholderName = updated.CardholderName,
            CardNumber = updated.CardNumber,
            Brand = updated.Brand,
            CreditLimit = updated.CreditLimit,
            CreatedAt = updated.CreatedAt
        };
    }
}
