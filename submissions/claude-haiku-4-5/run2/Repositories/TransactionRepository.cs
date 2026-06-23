using CreditCardApi.Data;
using CreditCardApi.Models;
using CreditCardApi.Repositories.Base;

namespace CreditCardApi.Repositories;

public class TransactionRepository(AppDbContext context) : RepositoryBase<Transaction>(context), ITransactionRepository
{
}
