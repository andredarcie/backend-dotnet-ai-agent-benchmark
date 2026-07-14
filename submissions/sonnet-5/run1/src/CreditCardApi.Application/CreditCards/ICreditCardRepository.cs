using CreditCardApi.Domain.Entities;

namespace CreditCardApi.Application.CreditCards;

public interface ICreditCardRepository
{
    Task<CreditCard?> GetByIdAsync(int id, CancellationToken cancellationToken);

    Task<IReadOnlyList<CreditCard>> GetPagedAsync(int pageNumber, int pageSize, CancellationToken cancellationToken);

    Task<bool> ExistsAsync(int id, CancellationToken cancellationToken);

    void Add(CreditCard creditCard);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
