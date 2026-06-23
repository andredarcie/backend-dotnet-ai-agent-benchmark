using CreditCardApi.Models;

namespace CreditCardApi.Data.Repositories;

public class TransactionRepository : RepositoryBase<Transaction>, ITransactionRepository
{
    public TransactionRepository(CreditCardDbContext context) : base(context)
    {
    }
}
