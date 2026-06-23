using CreditCardApi.Models;

namespace CreditCardApi.Data.Repositories;

public interface ICreditCardRepository : IRepository<CreditCard>
{
    Task<CreditCard?> GetByIdWithTransactionsAsync(int id);
}
