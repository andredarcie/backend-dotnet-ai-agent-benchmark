using CreditCardApi.Data.Repositories;
using CreditCardApi.Models;

namespace CreditCardApi.UseCases;

public class GetTransactionByIdUseCase
{
    private readonly ITransactionRepository _repository;

    public GetTransactionByIdUseCase(ITransactionRepository repository)
        => _repository = repository;

    public async Task<Transaction?> ExecuteAsync(int id)
        => await _repository.GetByIdAsync(id);
}
