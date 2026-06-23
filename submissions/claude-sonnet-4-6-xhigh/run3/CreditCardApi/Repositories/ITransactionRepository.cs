using CreditCardApi.Models;
using CreditCardApi.Repositories.Base;

namespace CreditCardApi.Repositories;

public interface ITransactionRepository : IRepositoryBase<Transaction>
{
    Task<IEnumerable<Transaction>> GetByCardIdAsync(int creditCardId);
}
