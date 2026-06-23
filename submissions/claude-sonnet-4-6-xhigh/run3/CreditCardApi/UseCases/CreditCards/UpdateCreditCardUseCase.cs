using CreditCardApi.DTOs;
using CreditCardApi.Models;
using CreditCardApi.Repositories;

namespace CreditCardApi.UseCases.CreditCards;

public class UpdateCreditCardUseCase
{
    private readonly ICreditCardRepository _repository;

    public UpdateCreditCardUseCase(ICreditCardRepository repository)
    {
        _repository = repository;
    }

    public async Task<(CreditCard? Card, string? Error, bool NotFound)> ExecuteAsync(int id, UpdateCreditCardDto dto)
    {
        var card = await _repository.GetByIdAsync(id);
        if (card == null)
            return (null, null, true);

        if (string.IsNullOrWhiteSpace(dto.CardholderName))
            return (null, "cardholderName is required", false);

        if (string.IsNullOrWhiteSpace(dto.CardNumber))
            return (null, "cardNumber is required", false);

        if (dto.CreditLimit < 0)
            return (null, "creditLimit must be >= 0", false);

        card.CardholderName = dto.CardholderName.Trim();
        card.CardNumber = dto.CardNumber.Trim();
        card.Brand = dto.Brand?.Trim();
        card.CreditLimit = dto.CreditLimit;

        _repository.Update(card);
        await _repository.SaveChangesAsync();

        return (card, null, false);
    }
}
