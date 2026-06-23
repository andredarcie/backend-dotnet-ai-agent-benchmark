using CreditCardApi.DTOs;
using CreditCardApi.Models;
using CreditCardApi.Repositories;

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

    public async Task<(Transaction? Transaction, string? Error, bool NotFound)> ExecuteAsync(int id, UpdateTransactionDto dto)
    {
        var transaction = await _transactionRepository.GetByIdAsync(id);
        if (transaction == null)
            return (null, null, true);

        if (string.IsNullOrWhiteSpace(dto.Merchant))
            return (null, "merchant is required", false);

        if (dto.Amount <= 0)
            return (null, "amount must be > 0", false);

        var card = await _creditCardRepository.GetByIdAsync(dto.CreditCardId);
        if (card == null)
            return (null, $"creditCardId {dto.CreditCardId} does not exist", false);

        transaction.CreditCardId = dto.CreditCardId;
        transaction.Amount = dto.Amount;
        transaction.Merchant = dto.Merchant.Trim();
        transaction.Category = dto.Category?.Trim();

        _transactionRepository.Update(transaction);
        await _transactionRepository.SaveChangesAsync();

        return (transaction, null, false);
    }
}
