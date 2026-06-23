using CreditCardApi.Models;
using Microsoft.EntityFrameworkCore;

namespace CreditCardApi.Data.Repositories;

public class CreditCardRepository : RepositoryBase<CreditCard>, ICreditCardRepository
{
    public CreditCardRepository(CreditCardDbContext context) : base(context) { }

    public async Task<CreditCard?> GetByIdWithTransactionsAsync(int id)
        => await _dbSet.Include(c => c.Transactions)
                       .FirstOrDefaultAsync(c => c.Id == id);
}
