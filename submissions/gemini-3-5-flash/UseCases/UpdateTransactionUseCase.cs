using System;
using System.Threading.Tasks;
using Gemini.Data.Repositories;
using Gemini.Models;

namespace Gemini.UseCases;

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

    public async Task<Transaction?> ExecuteAsync(int id, Transaction updatedTransaction)
    {
        var existingTransaction = await _transactionRepository.GetByIdAsync(id);
        if (existingTransaction == null)
        {
            return null;
        }

        // Validation rules
        if (string.IsNullOrWhiteSpace(updatedTransaction.Merchant))
        {
            throw new ArgumentException("Merchant is required.");
        }

        if (updatedTransaction.Amount <= 0)
        {
            throw new ArgumentException("Amount must be greater than 0.");
        }

        var cardExists = await _creditCardRepository.ExistsAsync(updatedTransaction.CreditCardId);
        if (!cardExists)
        {
            throw new ArgumentException("CreditCard does not exist.");
        }

        existingTransaction.Merchant = updatedTransaction.Merchant;
        existingTransaction.Amount = updatedTransaction.Amount;
        existingTransaction.Category = updatedTransaction.Category;
        existingTransaction.CreditCardId = updatedTransaction.CreditCardId;
        // Do not update existingTransaction.CreatedAt

        _transactionRepository.Update(existingTransaction);
        await _transactionRepository.SaveChangesAsync();

        return existingTransaction;
    }
}
