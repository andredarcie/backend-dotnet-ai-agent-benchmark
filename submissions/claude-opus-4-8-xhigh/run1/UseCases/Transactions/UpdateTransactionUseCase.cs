using CreditCardApi.Application;
using CreditCardApi.DTOs;
using CreditCardApi.Repositories;

namespace CreditCardApi.UseCases.Transactions;

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

    public async Task<Result<TransactionResponse>> ExecuteAsync(int id, UpdateTransactionRequest request, CancellationToken ct = default)
    {
        var transaction = await _transactionRepository.GetByIdAsync(id, ct);
        if (transaction is null)
        {
            return Result<TransactionResponse>.NotFound();
        }

        if (string.IsNullOrWhiteSpace(request.Merchant))
        {
            return Result<TransactionResponse>.Invalid("merchant is required.");
        }

        if (request.Amount <= 0)
        {
            return Result<TransactionResponse>.Invalid("amount must be greater than 0.");
        }

        if (!await _creditCardRepository.ExistsAsync(request.CreditCardId, ct))
        {
            return Result<TransactionResponse>.Invalid("creditCardId must reference an existing credit card.");
        }

        transaction.CreditCardId = request.CreditCardId;
        transaction.Amount = request.Amount;
        transaction.Merchant = request.Merchant.Trim();
        transaction.Category = request.Category;

        await _transactionRepository.UpdateAsync(transaction, ct);
        return Result<TransactionResponse>.Success(transaction.ToResponse());
    }
}
