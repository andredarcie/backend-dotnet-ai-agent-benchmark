using CreditCardApi.Data;
using CreditCardApi.Models;

namespace CreditCardApi.Repositories;

public class TransactionRepository : RepositoryBase<Transaction>, ITransactionRepository
{
    public TransactionRepository(AppDbContext context) : base(context)
    {
    }
}
