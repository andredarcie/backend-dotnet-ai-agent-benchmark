using System.Collections.Generic;
using System.Threading.Tasks;
using CreditCardApi.Domain;

namespace CreditCardApi.Repositories
{
    public interface ITransactionRepository : IRepositoryBase<Transaction>
    {
        Task<IEnumerable<Transaction>> GetByCreditCardIdAsync(int creditCardId);
    }
}
