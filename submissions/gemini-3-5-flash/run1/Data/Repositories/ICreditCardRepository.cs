using System.Collections.Generic;
using System.Threading.Tasks;
using Gemini.Models;

namespace Gemini.Data.Repositories;

public interface ICreditCardRepository : IRepositoryBase<CreditCard>
{
    Task<bool> ExistsAsync(int id);
    Task<IEnumerable<Transaction>> GetTransactionsByCardIdAsync(int cardId);
}
