using CreditCardApi.Data.Repositories;
using CreditCardApi.DTOs;
using CreditCardApi.Models;

namespace CreditCardApi.UseCases;

public class CreateCreditCardUseCase
{
    private readonly ICreditCardRepository _repository;

    public CreateCreditCardUseCase(ICreditCardRepository repository)
    {
        _repository = repository;
    }

    public async Task<CreditCardResponse> ExecuteAsync(CreateCreditCardRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.CardholderName))
            throw new ArgumentException("Cardholder name is required");

        if (string.IsNullOrWhiteSpace(request.CardNumber))
            throw new ArgumentException("Card number is required");

        var creditCard = new CreditCard
        {
            CardholderName = request.CardholderName,
            CardNumber = request.CardNumber,
            Brand = request.Brand,
            CreditLimit = request.CreditLimit,
            CreatedAt = DateTime.UtcNow
        };

        await _repository.AddAsync(creditCard);
        await _repository.SaveChangesAsync();

        return MapToResponse(creditCard);
    }

    private CreditCardResponse MapToResponse(CreditCard creditCard)
    {
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
