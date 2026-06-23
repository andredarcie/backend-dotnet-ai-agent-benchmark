using CreditCardApi.Models;

namespace CreditCardApi.Repositories;

public interface ICreditCardRepository
{
    Task<CreditCard?> GetByIdAsync(int id);
    Task<IEnumerable<CreditCard>> GetAllAsync();
    Task<CreditCard> AddAsync(CreditCard entity);
    Task<CreditCard> UpdateAsync(CreditCard entity);
    Task DeleteAsync(CreditCard entity);
}
