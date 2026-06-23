using CreditCardApi.Data;
using CreditCardApi.Models;

namespace CreditCardApi.Repositories;

public class CreditCardRepository : RepositoryBase<CreditCard>, ICreditCardRepository
{
    public CreditCardRepository(AppDbContext dbContext) : base(dbContext)
    {
    }
}
