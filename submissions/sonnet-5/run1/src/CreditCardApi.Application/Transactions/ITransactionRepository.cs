using CreditCardApi.Domain.Entities;

namespace CreditCardApi.Application.Transactions;

public interface ITransactionRepository
{
    Task<Transaction?> GetByIdAsync(int id, CancellationToken cancellationToken);

    Task<IReadOnlyList<Transaction>> GetPagedAsync(int pageNumber, int pageSize, CancellationToken cancellationToken);

    Task<IReadOnlyList<Transaction>> GetByCreditCardIdAsync(int creditCardId, CancellationToken cancellationToken);

    void Add(Transaction transaction);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
