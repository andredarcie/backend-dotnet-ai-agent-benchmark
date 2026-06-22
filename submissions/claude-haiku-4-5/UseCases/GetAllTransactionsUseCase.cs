using CreditCardApi.Data.Repositories;
using CreditCardApi.DTOs;

namespace CreditCardApi.UseCases;

public class GetAllTransactionsUseCase
{
    private readonly ITransactionRepository _repository;

    public GetAllTransactionsUseCase(ITransactionRepository repository)
    {
        _repository = repository;
    }

    public async Task<IEnumerable<TransactionResponse>> ExecuteAsync()
    {
        var transactions = await _repository.GetAllAsync();
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
