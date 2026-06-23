using CreditCardApi.Domain;
using CreditCardApi.Infrastructure.Data;

namespace CreditCardApi.Repositories
{
    public class CreditCardRepository : RepositoryBase<CreditCard>, ICreditCardRepository
    {
        public CreditCardRepository(AppDbContext context) : base(context)
        {
        }
    }
}
