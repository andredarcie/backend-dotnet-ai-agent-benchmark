using CreditCardApi.DTOs;
using CreditCardApi.Repositories;

namespace CreditCardApi.UseCases;

public class GetCreditCardTransactionsUseCase
{
    private readonly ICreditCardRepository _creditCardRepository;
    private readonly ITransactionRepository _transactionRepository;

    public GetCreditCardTransactionsUseCase(
        ICreditCardRepository creditCardRepository,
        ITransactionRepository transactionRepository)
    {
        _creditCardRepository = creditCardRepository;
        _transactionRepository = transactionRepository;
    }

    public async Task<IEnumerable<TransactionResponse>?> ExecuteAsync(int creditCardId)
    {
        var creditCard = await _creditCardRepository.GetByIdAsync(creditCardId);
        if (creditCard == null)
            return null;

        var transactions = await _transactionRepository.GetByCreditCardIdAsync(creditCardId);
        return transactions.Select(t => new TransactionResponse
        {
            Id = t.Id,
            CreditCardId = t.CreditCardId,
            Amount = t.Amount,
            Merchant = t.Merchant,
            Category = t.Category,
            CreatedAt = t.CreatedAt
        });
    }
}
