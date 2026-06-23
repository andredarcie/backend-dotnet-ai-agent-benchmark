using CreditCardApi.DTOs;
using CreditCardApi.Models;
using CreditCardApi.Repositories;

namespace CreditCardApi.UseCases;

public class UpdateTransactionUseCase(ITransactionRepository transactionRepository, ICreditCardRepository creditCardRepository)
{
    public async Task<Transaction?> ExecuteAsync(int id, UpdateTransactionDto dto)
    {
        var transaction = await transactionRepository.GetByIdAsync(id);
        if (transaction == null)
            return null;

        if (dto.CreditCardId.HasValue)
        {
            var creditCard = await creditCardRepository.GetByIdAsync(dto.CreditCardId.Value);
            if (creditCard == null)
                throw new ArgumentException($"CreditCard with id {dto.CreditCardId} not found");

            transaction.CreditCardId = dto.CreditCardId.Value;
        }

        if (dto.Amount.HasValue)
        {
            if (dto.Amount.Value <= 0)
                throw new ArgumentException("Amount must be greater than 0");
            transaction.Amount = dto.Amount.Value;
        }

        if (!string.IsNullOrWhiteSpace(dto.Merchant))
            transaction.Merchant = dto.Merchant;

        if (dto.Category != null)
            transaction.Category = dto.Category;

        await transactionRepository.UpdateAsync(transaction);
        await transactionRepository.SaveChangesAsync();

        return transaction;
    }
}
