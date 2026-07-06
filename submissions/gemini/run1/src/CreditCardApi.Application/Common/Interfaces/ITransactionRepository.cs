using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CreditCardApi.Domain.Entities;

namespace CreditCardApi.Application.Common.Interfaces;

/// <summary>
/// Repository interface for Transaction operations.
/// </summary>
public interface ITransactionRepository
{
    Task<Transaction?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IEnumerable<Transaction>> GetPagedAsync(int page, int pageSize, CancellationToken cancellationToken = default);
    Task<int> GetTotalCountAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<Transaction>> GetByCreditCardIdAsync(int creditCardId, CancellationToken cancellationToken = default);
    Task AddAsync(Transaction transaction, CancellationToken cancellationToken = default);
    Task DeleteAsync(Transaction transaction, CancellationToken cancellationToken = default);
}
