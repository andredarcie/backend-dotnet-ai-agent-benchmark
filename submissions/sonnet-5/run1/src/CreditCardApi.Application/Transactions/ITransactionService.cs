using CreditCardApi.Application.Transactions.Dtos;

namespace CreditCardApi.Application.Transactions;

public interface ITransactionService
{
    Task<TransactionResponse> CreateAsync(CreateTransactionRequest request, CancellationToken cancellationToken);

    Task<TransactionResponse> GetByIdAsync(int id, CancellationToken cancellationToken);

    Task<IReadOnlyList<TransactionResponse>> GetPagedAsync(int pageNumber, int pageSize, CancellationToken cancellationToken);
}
