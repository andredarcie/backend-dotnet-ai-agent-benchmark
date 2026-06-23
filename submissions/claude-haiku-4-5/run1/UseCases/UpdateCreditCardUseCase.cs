using CreditCardApi.Data.Repositories;
using CreditCardApi.DTOs;

namespace CreditCardApi.UseCases;

public class UpdateCreditCardUseCase
{
    private readonly ICreditCardRepository _repository;

    public UpdateCreditCardUseCase(ICreditCardRepository repository)
    {
        _repository = repository;
    }

    public async Task<CreditCardResponse?> ExecuteAsync(int id, CreateCreditCardRequest request)
    {
        var creditCard = await _repository.GetByIdAsync(id);
        if (creditCard == null)
            return null;

        if (string.IsNullOrWhiteSpace(request.CardholderName))
            throw new ArgumentException("Cardholder name is required");

        if (string.IsNullOrWhiteSpace(request.CardNumber))
            throw new ArgumentException("Card number is required");

        creditCard.CardholderName = request.CardholderName;
        creditCard.CardNumber = request.CardNumber;
        creditCard.Brand = request.Brand;
        creditCard.CreditLimit = request.CreditLimit;

        await _repository.UpdateAsync(creditCard);
        await _repository.SaveChangesAsync();

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
