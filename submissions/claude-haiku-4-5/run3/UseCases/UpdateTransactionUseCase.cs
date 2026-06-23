using CreditCardApi.DTOs;
using CreditCardApi.Repositories;

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

    public async Task<TransactionResponse?> ExecuteAsync(int id, UpdateTransactionRequest request)
    {
        var transaction = await _transactionRepository.GetByIdAsync(id);
        if (transaction == null)
            return null;

        if (request.CreditCardId.HasValue)
        {
            var creditCard = await _creditCardRepository.GetByIdAsync(request.CreditCardId.Value);
            if (creditCard == null)
                throw new ArgumentException("CreditCard not found");
            transaction.CreditCardId = request.CreditCardId.Value;
        }

        if (request.Amount.HasValue)
        {
            if (request.Amount.Value <= 0)
                throw new ArgumentException("Amount must be greater than 0");
            transaction.Amount = request.Amount.Value;
        }

        if (!string.IsNullOrWhiteSpace(request.Merchant))
            transaction.Merchant = request.Merchant;

        if (!string.IsNullOrWhiteSpace(request.Category))
            transaction.Category = request.Category;

        var updated = await _transactionRepository.UpdateAsync(transaction);

        return new TransactionResponse
        {
            Id = updated.Id,
            CreditCardId = updated.CreditCardId,
            Amount = updated.Amount,
            Merchant = updated.Merchant,
            Category = updated.Category,
            CreatedAt = updated.CreatedAt
        };
    }
}
