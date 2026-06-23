using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CreditCardApi.DTOs;
using CreditCardApi.Repositories;

namespace CreditCardApi.UseCases.CreditCards
{
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

        public async Task<UseCaseResult<IEnumerable<TransactionResponse>>> ExecuteAsync(int creditCardId)
        {
            var card = await _creditCardRepository.GetByIdAsync(creditCardId);
            if (card == null)
            {
                return UseCaseResult<IEnumerable<TransactionResponse>>.FailNotFound();
            }

            var transactions = await _transactionRepository.GetByCreditCardIdAsync(creditCardId);
            var response = transactions.Select(t => new TransactionResponse
            {
                Id = t.Id,
                CreditCardId = t.CreditCardId,
                Amount = t.Amount,
                Merchant = t.Merchant,
                Category = t.Category,
                CreatedAt = t.CreatedAt
            });

            return UseCaseResult<IEnumerable<TransactionResponse>>.Ok(response);
        }
    }
}
