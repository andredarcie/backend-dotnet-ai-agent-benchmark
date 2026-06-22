using CreditCardApi.Exceptions;
using CreditCardApi.Models;
using CreditCardApi.Repositories.Interfaces;

namespace CreditCardApi.UseCases.Transactions;

public class GetTransactionByIdUseCase
{
    private readonly ITransactionRepository _repository;

    public GetTransactionByIdUseCase(ITransactionRepository repository)
    {
        _repository = repository;
    }

    public async Task<Transaction> ExecuteAsync(int id)
    {
        var transaction = await _repository.GetByIdAsync(id);
        if (transaction == null)
            throw new NotFoundException($"Transaction with id {id} not found");
        return transaction;
    }
}
