using System.Threading.Tasks;
using Gemini.Models;
using Microsoft.EntityFrameworkCore;

namespace Gemini.Data.Repositories;

public class TransactionRepository : RepositoryBase<Transaction>, ITransactionRepository
{
    public TransactionRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<bool> ExistsAsync(int id)
    {
        return await DbSet.AnyAsync(t => t.Id == id);
    }
}
