using CreditCardApi.Domain.Entities;

namespace CreditCardApi.Application.Repositories;

public interface ITransactionRepository
{
    Task<Transaction?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IEnumerable<Transaction>> GetAllAsync(int pageNumber = 1, int pageSize = 10, CancellationToken cancellationToken = default);
    Task<IEnumerable<Transaction>> GetByCreditCardIdAsync(int creditCardId, CancellationToken cancellationToken = default);
    Task<Transaction> AddAsync(Transaction transaction, CancellationToken cancellationToken = default);
    Task UpdateAsync(Transaction transaction, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}
