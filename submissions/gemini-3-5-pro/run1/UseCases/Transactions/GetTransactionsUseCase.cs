using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CreditCardApi.DTOs;
using CreditCardApi.Repositories;

namespace CreditCardApi.UseCases.Transactions
{
    public class GetTransactionsUseCase
    {
        private readonly ITransactionRepository _transactionRepository;

        public GetTransactionsUseCase(ITransactionRepository transactionRepository)
        {
            _transactionRepository = transactionRepository;
        }

        public async Task<UseCaseResult<IEnumerable<TransactionResponse>>> ExecuteAsync()
        {
            var transactions = await _transactionRepository.GetAllAsync();
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
