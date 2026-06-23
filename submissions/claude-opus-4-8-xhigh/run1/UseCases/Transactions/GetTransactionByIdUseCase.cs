using CreditCardApi.Application;
using CreditCardApi.DTOs;
using CreditCardApi.Repositories;

namespace CreditCardApi.UseCases.Transactions;

public class GetTransactionByIdUseCase
{
    private readonly ITransactionRepository _repository;

    public GetTransactionByIdUseCase(ITransactionRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<TransactionResponse>> ExecuteAsync(int id, CancellationToken ct = default)
    {
        var transaction = await _repository.GetByIdAsync(id, ct);
        return transaction is null
            ? Result<TransactionResponse>.NotFound()
            : Result<TransactionResponse>.Success(transaction.ToResponse());
    }
}
