using CreditCardApi.Data.Repositories;
using CreditCardApi.DTOs;

namespace CreditCardApi.UseCases;

public class GetCreditCardTransactionsUseCase
{
    private readonly ICreditCardRepository _creditCardRepository;

    public GetCreditCardTransactionsUseCase(ICreditCardRepository creditCardRepository)
    {
        _creditCardRepository = creditCardRepository;
    }

    public async Task<IEnumerable<TransactionResponse>?> ExecuteAsync(int creditCardId)
    {
        var creditCard = await _creditCardRepository.GetByIdAsync(creditCardId);
        if (creditCard == null)
            return null;

        var transactions = await _creditCardRepository.GetTransactionsByCardIdAsync(creditCardId);
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
