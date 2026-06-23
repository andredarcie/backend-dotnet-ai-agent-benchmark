using CreditCardApi.Data.Repositories;
using CreditCardApi.Models;

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

    public async Task<(IEnumerable<Transaction>? Transactions, bool NotFound)> ExecuteAsync(int creditCardId)
    {
        var card = await _creditCardRepository.GetByIdAsync(creditCardId);
        if (card is null)
            return (null, true);

        var transactions = await _transactionRepository.GetByCreditCardIdAsync(creditCardId);
        return (transactions, false);
    }
}
