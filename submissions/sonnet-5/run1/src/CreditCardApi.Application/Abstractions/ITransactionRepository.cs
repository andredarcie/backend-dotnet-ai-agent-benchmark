using CreditCardApi.Domain.Entities;

namespace CreditCardApi.Application.Abstractions;

public interface ITransactionRepository
{
    /// <summary>Loads a tracked transaction for mutation (update/delete).</summary>
    Task<Transaction?> GetByIdAsync(int id, CancellationToken cancellationToken);

    /// <summary>Loads a transaction for a read-only response; must not attach it to the change tracker.</summary>
    Task<Transaction?> FindReadOnlyAsync(int id, CancellationToken cancellationToken);

    Task<(IReadOnlyList<Transaction> Items, int TotalCount)> ListReadOnlyAsync(
        int page, int pageSize, CancellationToken cancellationToken);

    Task<(IReadOnlyList<Transaction> Items, int TotalCount)> ListByCreditCardReadOnlyAsync(
        int creditCardId, int page, int pageSize, CancellationToken cancellationToken);

    void Add(Transaction transaction);

    void Remove(Transaction transaction);
}
