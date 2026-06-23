using CreditCardApi.Application;
using CreditCardApi.DTOs;
using CreditCardApi.Repositories;

namespace CreditCardApi.UseCases.Transactions;

public class GetAllTransactionsUseCase
{
    private readonly ITransactionRepository _repository;

    public GetAllTransactionsUseCase(ITransactionRepository repository)
    {
        _repository = repository;
    }

    public async Task<IReadOnlyList<TransactionResponse>> ExecuteAsync(CancellationToken ct = default)
    {
        var transactions = await _repository.GetAllAsync(ct);
        return transactions.Select(t => t.ToResponse()).ToList();
    }
}
