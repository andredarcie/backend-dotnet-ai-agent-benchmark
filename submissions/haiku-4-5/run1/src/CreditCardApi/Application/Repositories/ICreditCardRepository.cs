using CreditCardApi.Domain.Entities;

namespace CreditCardApi.Application.Repositories;

/// <summary>
/// Repository interface for credit card operations.
/// </summary>
public interface ICreditCardRepository
{
    Task<CreditCard?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IEnumerable<CreditCard>> GetAllAsync(int pageNumber = 1, int pageSize = 10, CancellationToken cancellationToken = default);
    Task<CreditCard> CreateAsync(CreditCard creditCard, CancellationToken cancellationToken = default);
    Task<CreditCard?> UpdateAsync(int id, CreditCard creditCard, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
}
