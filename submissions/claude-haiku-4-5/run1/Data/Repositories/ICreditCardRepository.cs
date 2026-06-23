using CreditCardApi.Models;

namespace CreditCardApi.Data.Repositories;

public interface ICreditCardRepository : IRepository<CreditCard>
{
    Task<IEnumerable<Transaction>> GetTransactionsByCardIdAsync(int creditCardId);
}
