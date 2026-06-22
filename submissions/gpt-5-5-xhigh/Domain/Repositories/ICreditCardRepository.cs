using CreditCardApi.Domain.Entities;

namespace CreditCardApi.Domain.Repositories;

public interface ICreditCardRepository : IRepository<CreditCard>
{
    Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default);
}
