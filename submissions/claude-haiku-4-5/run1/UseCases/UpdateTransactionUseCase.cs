using CreditCardApi.Data.Repositories;
using CreditCardApi.DTOs;

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

    public async Task<TransactionResponse?> ExecuteAsync(int id, CreateTransactionRequest request)
    {
        var transaction = await _transactionRepository.GetByIdAsync(id);
        if (transaction == null)
            return null;

        if (request.Amount <= 0)
            throw new ArgumentException("Amount must be greater than 0");

        if (string.IsNullOrWhiteSpace(request.Merchant))
            throw new ArgumentException("Merchant is required");

        var creditCard = await _creditCardRepository.GetByIdAsync(request.CreditCardId);
        if (creditCard == null)
            throw new ArgumentException("Credit card not found");

        transaction.CreditCardId = request.CreditCardId;
        transaction.Amount = request.Amount;
        transaction.Merchant = request.Merchant;
        transaction.Category = request.Category;

        await _transactionRepository.UpdateAsync(transaction);
        await _transactionRepository.SaveChangesAsync();

        return new TransactionResponse
        {
            Id = transaction.Id,
            CreditCardId = transaction.CreditCardId,
            Amount = transaction.Amount,
            Merchant = transaction.Merchant,
            Category = transaction.Category,
            CreatedAt = transaction.CreatedAt
        };
    }
}
