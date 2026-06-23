using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using CreditCardApi.Domain;
using CreditCardApi.Infrastructure.Data;

namespace CreditCardApi.Repositories
{
    public class TransactionRepository : RepositoryBase<Transaction>, ITransactionRepository
    {
        public TransactionRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<Transaction>> GetByCreditCardIdAsync(int creditCardId)
        {
            return await _dbSet
                .Where(t => t.CreditCardId == creditCardId)
                .ToListAsync();
        }
    }
}
