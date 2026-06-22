using CreditCardApi.DTOs;
using CreditCardApi.Exceptions;
using CreditCardApi.Models;
using CreditCardApi.Repositories.Interfaces;

namespace CreditCardApi.UseCases.CreditCards;

public class CreateCreditCardUseCase
{
    private readonly ICreditCardRepository _repository;

    public CreateCreditCardUseCase(ICreditCardRepository repository)
    {
        _repository = repository;
    }

    public async Task<CreditCard> ExecuteAsync(CreateCreditCardDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.CardholderName))
            throw new ValidationException("cardholderName is required and cannot be empty");

        if (string.IsNullOrWhiteSpace(dto.CardNumber))
            throw new ValidationException("cardNumber is required and cannot be empty");

        if (dto.CreditLimit < 0)
            throw new ValidationException("creditLimit must be >= 0");

        var card = new CreditCard
        {
            CardholderName = dto.CardholderName.Trim(),
            CardNumber = dto.CardNumber.Trim(),
            Brand = dto.Brand,
            CreditLimit = dto.CreditLimit,
            CreatedAt = DateTime.UtcNow
        };

        return await _repository.AddAsync(card);
    }
}
