using CreditCardApi.Data.Repositories;
using CreditCardApi.DTOs;
using CreditCardApi.Models;

namespace CreditCardApi.UseCases;

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

    public async Task<(Transaction? Transaction, string? Error, bool NotFound)> ExecuteAsync(int id, UpdateTransactionRequest request)
    {
        var transaction = await _transactionRepository.GetByIdAsync(id);
        if (transaction is null)
            return (null, null, true);

        if (string.IsNullOrWhiteSpace(request.Merchant))
            return (null, "merchant is required.", false);

        if (request.Amount <= 0)
            return (null, "amount must be > 0.", false);

        var card = await _creditCardRepository.GetByIdAsync(request.CreditCardId);
        if (card is null)
            return (null, $"creditCardId {request.CreditCardId} does not exist.", false);

        transaction.CreditCardId = request.CreditCardId;
        transaction.Amount = request.Amount;
        transaction.Merchant = request.Merchant;
        transaction.Category = request.Category;

        await _transactionRepository.UpdateAsync(transaction);
        return (transaction, null, false);
    }
}
