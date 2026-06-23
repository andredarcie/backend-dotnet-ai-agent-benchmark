using CreditCardApi.Data;
using CreditCardApi.Models;
using Microsoft.EntityFrameworkCore;

namespace CreditCardApi.Repositories;

public class CreditCardRepository : RepositoryBase<CreditCard>, ICreditCardRepository
{
    public CreditCardRepository(AppDbContext context) : base(context)
    {
    }

    public Task<bool> ExistsAsync(int id, CancellationToken ct = default)
        => DbSet.AnyAsync(c => c.Id == id, ct);

    public async Task<List<Transaction>?> GetTransactionsAsync(int creditCardId, CancellationToken ct = default)
    {
        if (!await ExistsAsync(creditCardId, ct))
        {
            return null;
        }

        return await Context.Transactions
            .AsNoTracking()
            .Where(t => t.CreditCardId == creditCardId)
            .OrderBy(t => t.Id)
            .ToListAsync(ct);
    }
}
