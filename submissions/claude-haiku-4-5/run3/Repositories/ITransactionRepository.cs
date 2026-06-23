using CreditCardApi.Models;

namespace CreditCardApi.Repositories;

public interface ITransactionRepository
{
    Task<Transaction?> GetByIdAsync(int id);
    Task<IEnumerable<Transaction>> GetAllAsync();
    Task<IEnumerable<Transaction>> GetByCreditCardIdAsync(int creditCardId);
    Task<Transaction> AddAsync(Transaction entity);
    Task<Transaction> UpdateAsync(Transaction entity);
    Task DeleteAsync(Transaction entity);
}
