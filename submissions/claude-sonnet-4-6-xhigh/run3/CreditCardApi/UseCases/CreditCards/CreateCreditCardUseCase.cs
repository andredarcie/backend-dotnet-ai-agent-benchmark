using CreditCardApi.DTOs;
using CreditCardApi.Models;
using CreditCardApi.Repositories;

namespace CreditCardApi.UseCases.CreditCards;

public class CreateCreditCardUseCase
{
    private readonly ICreditCardRepository _repository;

    public CreateCreditCardUseCase(ICreditCardRepository repository)
    {
        _repository = repository;
    }

    public async Task<(CreditCard? Card, string? Error)> ExecuteAsync(CreateCreditCardDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.CardholderName))
            return (null, "cardholderName is required");

        if (string.IsNullOrWhiteSpace(dto.CardNumber))
            return (null, "cardNumber is required");

        if (dto.CreditLimit < 0)
            return (null, "creditLimit must be >= 0");

        var card = new CreditCard
        {
            CardholderName = dto.CardholderName.Trim(),
            CardNumber = dto.CardNumber.Trim(),
            Brand = dto.Brand?.Trim(),
            CreditLimit = dto.CreditLimit,
            CreatedAt = DateTime.UtcNow
        };

        await _repository.AddAsync(card);
        await _repository.SaveChangesAsync();

        return (card, null);
    }
}
