using System.Threading.Tasks;
using CreditCardApi.DTOs;
using CreditCardApi.Repositories;

namespace CreditCardApi.UseCases.Transactions
{
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

        public async Task<UseCaseResult<TransactionResponse>> ExecuteAsync(int id, UpdateTransactionRequest request)
        {
            var transaction = await _transactionRepository.GetByIdAsync(id);
            if (transaction == null)
            {
                return UseCaseResult<TransactionResponse>.FailNotFound();
            }

            if (string.IsNullOrWhiteSpace(request.Merchant))
            {
                return UseCaseResult<TransactionResponse>.Fail("Merchant is required and cannot be empty.");
            }

            if (request.Amount <= 0)
            {
                return UseCaseResult<TransactionResponse>.Fail("Amount must be greater than 0.");
            }

            var card = await _creditCardRepository.GetByIdAsync(request.CreditCardId);
            if (card == null)
            {
                return UseCaseResult<TransactionResponse>.Fail("Credit card does not exist.");
            }

            transaction.CreditCardId = request.CreditCardId;
            transaction.Amount = request.Amount;
            transaction.Merchant = request.Merchant.Trim();
            transaction.Category = request.Category?.Trim();

            await _transactionRepository.UpdateAsync(transaction);

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
