using CreditCardApi.Application.Repositories;
using CreditCardApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CreditCardApi.Data.Repositories;

public class TransactionRepository(ApplicationDbContext context) : ITransactionRepository
{
    public async Task<Transaction?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await context.Transactions
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<Transaction>> GetAllAsync(int pageNumber = 1, int pageSize = 10, CancellationToken cancellationToken = default)
    {
        return await context.Transactions
            .AsNoTracking()
            .OrderByDescending(t => t.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Transaction>> GetByCreditCardIdAsync(int creditCardId, CancellationToken cancellationToken = default)
    {
        return await context.Transactions
            .AsNoTracking()
            .Where(t => t.CreditCardId == creditCardId)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<Transaction> CreateAsync(Transaction transaction, CancellationToken cancellationToken = default)
    {
        context.Transactions.Add(transaction);
        await context.SaveChangesAsync(cancellationToken);
        return transaction;
    }

    public async Task<Transaction?> UpdateAsync(int id, Transaction updatedTransaction, CancellationToken cancellationToken = default)
    {
        var existingTransaction = await context.Transactions.FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
        if (existingTransaction == null)
        {
            return null;
        }

        existingTransaction.CreditCardId = updatedTransaction.CreditCardId;
        existingTransaction.Amount = updatedTransaction.Amount;
        existingTransaction.Merchant = updatedTransaction.Merchant;
        existingTransaction.Category = updatedTransaction.Category;

        await context.SaveChangesAsync(cancellationToken);
        return existingTransaction;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var transaction = await context.Transactions.FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
        if (transaction == null)
        {
            return false;
        }

        context.Transactions.Remove(transaction);
        await context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
