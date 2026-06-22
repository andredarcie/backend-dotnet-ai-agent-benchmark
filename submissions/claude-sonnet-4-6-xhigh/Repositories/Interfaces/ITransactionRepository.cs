using CreditCardApi.Models;
using CreditCardApi.Repositories.Base;

namespace CreditCardApi.Repositories.Interfaces;

public interface ITransactionRepository : IRepositoryBase<Transaction>
{
    Task<IEnumerable<Transaction>> GetByCreditCardIdAsync(int creditCardId);
}
