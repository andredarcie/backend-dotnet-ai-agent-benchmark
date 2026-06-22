using CreditCardApi.Models;
using CreditCardApi.Repositories.Base;

namespace CreditCardApi.Repositories.Interfaces;

public interface ICreditCardRepository : IRepositoryBase<CreditCard>
{
    Task<bool> ExistsAsync(int id);
}
