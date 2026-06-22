using CreditCardApi.DTOs;
using CreditCardApi.Exceptions;
using CreditCardApi.Models;
using CreditCardApi.Repositories.Interfaces;

namespace CreditCardApi.UseCases.CreditCards;

public class UpdateCreditCardUseCase
{
    private readonly ICreditCardRepository _repository;

    public UpdateCreditCardUseCase(ICreditCardRepository repository)
    {
        _repository = repository;
    }

    public async Task<CreditCard> ExecuteAsync(int id, UpdateCreditCardDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.CardholderName))
            throw new ValidationException("cardholderName is required and cannot be empty");

        if (string.IsNullOrWhiteSpace(dto.CardNumber))
            throw new ValidationException("cardNumber is required and cannot be empty");

        if (dto.CreditLimit < 0)
            throw new ValidationException("creditLimit must be >= 0");

        var existing = await _repository.GetByIdAsync(id);
        if (existing == null)
            throw new NotFoundException($"Credit card with id {id} not found");

        existing.CardholderName = dto.CardholderName.Trim();
        existing.CardNumber = dto.CardNumber.Trim();
        existing.Brand = dto.Brand;
        existing.CreditLimit = dto.CreditLimit;

        return await _repository.UpdateAsync(existing);
    }
}
