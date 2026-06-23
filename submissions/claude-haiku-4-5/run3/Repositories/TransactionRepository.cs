using Microsoft.EntityFrameworkCore;
using CreditCardApi.Data;
using CreditCardApi.Models;

namespace CreditCardApi.Repositories;

public class TransactionRepository : RepositoryBase<Transaction>, ITransactionRepository
{
    public TransactionRepository(AppDbContext dbContext) : base(dbContext)
    {
    }

    public async Task<IEnumerable<Transaction>> GetByCreditCardIdAsync(int creditCardId)
    {
        return await _dbContext.Transactions
            .Where(t => t.CreditCardId == creditCardId)
            .ToListAsync();
    }
}
