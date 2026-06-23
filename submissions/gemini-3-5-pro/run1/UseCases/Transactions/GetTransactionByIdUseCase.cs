using System.Threading.Tasks;
using CreditCardApi.DTOs;
using CreditCardApi.Repositories;

namespace CreditCardApi.UseCases.Transactions
{
    public class GetTransactionByIdUseCase
    {
        private readonly ITransactionRepository _transactionRepository;

        public GetTransactionByIdUseCase(ITransactionRepository transactionRepository)
        {
            _transactionRepository = transactionRepository;
        }

        public async Task<UseCaseResult<TransactionResponse>> ExecuteAsync(int id)
        {
            var transaction = await _transactionRepository.GetByIdAsync(id);
            if (transaction == null)
            {
                return UseCaseResult<TransactionResponse>.FailNotFound();
            }

            var response = new TransactionResponse
            {
                Id = transaction.Id,
                CreditCardId = transaction.CreditCardId,
                Amount = transaction.Amount,
                Merchant = transaction.Merchant,
                Category = transaction.Category,
                CreatedAt = transaction.CreatedAt
            };

            return UseCaseResult<TransactionResponse>.Ok(response);
        }
    }
}
