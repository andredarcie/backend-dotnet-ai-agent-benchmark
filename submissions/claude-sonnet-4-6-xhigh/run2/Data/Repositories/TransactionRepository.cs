using CreditCardApi.Models;
using Microsoft.EntityFrameworkCore;

namespace CreditCardApi.Data.Repositories;

public class TransactionRepository : RepositoryBase<Transaction>, ITransactionRepository
{
    public TransactionRepository(CreditCardDbContext context) : base(context) { }

    public async Task<IEnumerable<Transaction>> GetByCreditCardIdAsync(int creditCardId)
        => await _dbSet.Where(t => t.CreditCardId == creditCardId).ToListAsync();
}
