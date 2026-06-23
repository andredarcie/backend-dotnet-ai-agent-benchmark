using CreditCardApi.Models;
using CreditCardApi.Repositories;

namespace CreditCardApi.UseCases.CreditCards;

public class GetTransactionsByCardIdUseCase
{
    private readonly ICreditCardRepository _creditCardRepository;
    private readonly ITransactionRepository _transactionRepository;

    public GetTransactionsByCardIdUseCase(
        ICreditCardRepository creditCardRepository,
        ITransactionRepository transactionRepository)
    {
        _creditCardRepository = creditCardRepository;
        _transactionRepository = transactionRepository;
    }

    public async Task<(IEnumerable<Transaction>? Transactions, bool NotFound)> ExecuteAsync(int cardId)
    {
        var card = await _creditCardRepository.GetByIdAsync(cardId);
        if (card == null)
            return (null, true);

        var transactions = await _transactionRepository.GetByCardIdAsync(cardId);
        return (transactions, false);
    }
}
