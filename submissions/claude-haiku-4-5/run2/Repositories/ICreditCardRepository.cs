using CreditCardApi.Models;
using CreditCardApi.Repositories.Base;

namespace CreditCardApi.Repositories;

public interface ICreditCardRepository : IRepositoryBase<CreditCard>
{
    Task<IEnumerable<Transaction>> GetTransactionsByCreditCardIdAsync(int creditCardId);
}
