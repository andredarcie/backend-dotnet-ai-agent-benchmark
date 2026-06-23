using CreditCardApi.DTOs;
using CreditCardApi.Models;
using CreditCardApi.Repositories;

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
        if (string.IsNullOrWhiteSpace(request.CardholderName) || string.IsNullOrWhiteSpace(request.CardNumber))
            throw new ArgumentException("CardholderName and CardNumber are required");

        var creditCard = new CreditCard
        {
            CardholderName = request.CardholderName,
            CardNumber = request.CardNumber,
            Brand = request.Brand,
            CreditLimit = request.CreditLimit,
            CreatedAt = DateTime.UtcNow
        };

        var created = await _repository.AddAsync(creditCard);

        return new CreditCardResponse
        {
            Id = created.Id,
            CardholderName = created.CardholderName,
            CardNumber = created.CardNumber,
            Brand = created.Brand,
            CreditLimit = created.CreditLimit,
            CreatedAt = created.CreatedAt
        };
    }
}
