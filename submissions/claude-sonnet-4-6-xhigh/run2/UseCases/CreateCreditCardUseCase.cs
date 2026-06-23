using CreditCardApi.Data.Repositories;
using CreditCardApi.DTOs;
using CreditCardApi.Models;

namespace CreditCardApi.UseCases;

public class CreateCreditCardUseCase
{
    private readonly ICreditCardRepository _repository;

    public CreateCreditCardUseCase(ICreditCardRepository repository)
        => _repository = repository;

    public async Task<(CreditCard? Card, string? Error)> ExecuteAsync(CreateCreditCardRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.CardholderName) || string.IsNullOrWhiteSpace(request.CardNumber))
            return (null, "cardholderName and cardNumber are required.");

        if (request.CreditLimit < 0)
            return (null, "creditLimit must be >= 0.");

        var card = new CreditCard
        {
            CardholderName = request.CardholderName,
            CardNumber = request.CardNumber,
            Brand = request.Brand,
            CreditLimit = request.CreditLimit,
            CreatedAt = DateTime.UtcNow
        };

        await _repository.AddAsync(card);
        return (card, null);
    }
}
