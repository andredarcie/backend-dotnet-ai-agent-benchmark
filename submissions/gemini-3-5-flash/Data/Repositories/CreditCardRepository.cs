using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Gemini.Models;
using Microsoft.EntityFrameworkCore;

namespace Gemini.Data.Repositories;

public class CreditCardRepository : RepositoryBase<CreditCard>, ICreditCardRepository
{
    public CreditCardRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<bool> ExistsAsync(int id)
    {
        return await DbSet.AnyAsync(c => c.Id == id);
    }

    public async Task<IEnumerable<Transaction>> GetTransactionsByCardIdAsync(int cardId)
    {
        return await Context.Transactions
            .Where(t => t.CreditCardId == cardId)
            .ToListAsync();
    }
}
