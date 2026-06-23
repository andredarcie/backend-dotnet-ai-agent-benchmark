using CreditCardApi.Data.Repositories;
using CreditCardApi.Models;

namespace CreditCardApi.UseCases;

public class GetAllTransactionsUseCase
{
    private readonly ITransactionRepository _repository;

    public GetAllTransactionsUseCase(ITransactionRepository repository)
        => _repository = repository;

    public async Task<IEnumerable<Transaction>> ExecuteAsync()
        => await _repository.GetAllAsync();
}
