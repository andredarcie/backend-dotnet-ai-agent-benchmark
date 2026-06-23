using CreditCardApi.Data.Repositories;
using CreditCardApi.DTOs;
using CreditCardApi.Models;

namespace CreditCardApi.UseCases;

public class UpdateCreditCardUseCase
{
    private readonly ICreditCardRepository _repository;

    public UpdateCreditCardUseCase(ICreditCardRepository repository)
        => _repository = repository;

    public async Task<(CreditCard? Card, string? Error, bool NotFound)> ExecuteAsync(int id, UpdateCreditCardRequest request)
    {
        var card = await _repository.GetByIdAsync(id);
        if (card is null)
            return (null, null, true);

        if (string.IsNullOrWhiteSpace(request.CardholderName) || string.IsNullOrWhiteSpace(request.CardNumber))
            return (null, "cardholderName and cardNumber are required.", false);

        if (request.CreditLimit < 0)
            return (null, "creditLimit must be >= 0.", false);

        card.CardholderName = request.CardholderName;
        card.CardNumber = request.CardNumber;
        card.Brand = request.Brand;
        card.CreditLimit = request.CreditLimit;

        await _repository.UpdateAsync(card);
        return (card, null, false);
    }
}
