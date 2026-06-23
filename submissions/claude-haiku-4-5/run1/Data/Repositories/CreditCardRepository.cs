using CreditCardApi.Models;
using Microsoft.EntityFrameworkCore;

namespace CreditCardApi.Data.Repositories;

public class CreditCardRepository : RepositoryBase<CreditCard>, ICreditCardRepository
{
    public CreditCardRepository(CreditCardDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<Transaction>> GetTransactionsByCardIdAsync(int creditCardId)
    {
        return await Context.Transactions
            .Where(t => t.CreditCardId == creditCardId)
            .ToListAsync();
    }
}
