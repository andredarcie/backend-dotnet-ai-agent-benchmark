using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CreditCardApi.Domain.Entities;

namespace CreditCardApi.Application.Common.Interfaces;

/// <summary>
/// Repository interface for CreditCard operations.
/// </summary>
public interface ICreditCardRepository
{
    Task<CreditCard?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IEnumerable<CreditCard>> GetPagedAsync(int page, int pageSize, CancellationToken cancellationToken = default);
    Task<int> GetTotalCountAsync(CancellationToken cancellationToken = default);
    Task AddAsync(CreditCard creditCard, CancellationToken cancellationToken = default);
    Task DeleteAsync(CreditCard creditCard, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default);
}
