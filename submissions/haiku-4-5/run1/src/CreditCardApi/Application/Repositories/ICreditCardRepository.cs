using CreditCardApi.Domain.Entities;

namespace CreditCardApi.Application.Repositories;

public interface ICreditCardRepository
{
    Task<CreditCard?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IEnumerable<CreditCard>> GetAllAsync(int pageNumber = 1, int pageSize = 10, CancellationToken cancellationToken = default);
    Task<CreditCard> AddAsync(CreditCard creditCard, CancellationToken cancellationToken = default);
    Task UpdateAsync(CreditCard creditCard, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}
