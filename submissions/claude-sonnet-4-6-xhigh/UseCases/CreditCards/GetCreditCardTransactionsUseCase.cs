using CreditCardApi.Exceptions;
using CreditCardApi.Models;
using CreditCardApi.Repositories.Interfaces;

namespace CreditCardApi.UseCases.CreditCards;

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

    public async Task<IEnumerable<Transaction>> ExecuteAsync(int creditCardId)
    {
        var cardExists = await _creditCardRepository.ExistsAsync(creditCardId);
        if (!cardExists)
            throw new NotFoundException($"Credit card with id {creditCardId} not found");

        return await _transactionRepository.GetByCreditCardIdAsync(creditCardId);
    }
}
