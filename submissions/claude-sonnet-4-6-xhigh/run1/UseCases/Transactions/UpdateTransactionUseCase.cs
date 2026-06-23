using CreditCardApi.DTOs;
using CreditCardApi.Exceptions;
using CreditCardApi.Models;
using CreditCardApi.Repositories.Interfaces;

namespace CreditCardApi.UseCases.Transactions;

public class UpdateTransactionUseCase
{
    private readonly ITransactionRepository _transactionRepository;
    private readonly ICreditCardRepository _creditCardRepository;

    public UpdateTransactionUseCase(
        ITransactionRepository transactionRepository,
        ICreditCardRepository creditCardRepository)
    {
        _transactionRepository = transactionRepository;
        _creditCardRepository = creditCardRepository;
    }

    public async Task<Transaction> ExecuteAsync(int id, UpdateTransactionDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Merchant))
            throw new ValidationException("merchant is required and cannot be empty");

        if (dto.Amount <= 0)
            throw new ValidationException("amount must be > 0");

        var existing = await _transactionRepository.GetByIdAsync(id);
        if (existing == null)
            throw new NotFoundException($"Transaction with id {id} not found");

        var cardExists = await _creditCardRepository.ExistsAsync(dto.CreditCardId);
        if (!cardExists)
            throw new ValidationException($"Credit card with id {dto.CreditCardId} does not exist");

        existing.CreditCardId = dto.CreditCardId;
        existing.Amount = dto.Amount;
        existing.Merchant = dto.Merchant.Trim();
        existing.Category = dto.Category;

        return await _transactionRepository.UpdateAsync(existing);
    }
}
