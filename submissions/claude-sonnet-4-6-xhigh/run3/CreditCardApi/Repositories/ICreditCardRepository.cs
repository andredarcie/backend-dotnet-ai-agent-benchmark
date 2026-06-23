using CreditCardApi.Models;
using CreditCardApi.Repositories.Base;

namespace CreditCardApi.Repositories;

public interface ICreditCardRepository : IRepositoryBase<CreditCard>
{
    Task<CreditCard?> GetByIdWithTransactionsAsync(int id);
}
