using System.Threading.Tasks;
using Gemini.Models;

namespace Gemini.Data.Repositories;

public interface ITransactionRepository : IRepositoryBase<Transaction>
{
    Task<bool> ExistsAsync(int id);
}
