using CreditCardApi.Data.Repositories;
using CreditCardApi.DTOs;

namespace CreditCardApi.UseCases;

public class GetTransactionByIdUseCase
{
    private readonly ITransactionRepository _repository;

    public GetTransactionByIdUseCase(ITransactionRepository repository)
    {
        _repository = repository;
    }

    public async Task<TransactionResponse?> ExecuteAsync(int id)
    {
        var transaction = await _repository.GetByIdAsync(id);
        if (transaction == null)
            return null;

        return new TransactionResponse
        {
            Id = transaction.Id,
            CreditCardId = transaction.CreditCardId,
            Amount = transaction.Amount,
            Merchant = transaction.Merchant,
            Category = transaction.Category,
            CreatedAt = transaction.CreatedAt
        };
    }
}
