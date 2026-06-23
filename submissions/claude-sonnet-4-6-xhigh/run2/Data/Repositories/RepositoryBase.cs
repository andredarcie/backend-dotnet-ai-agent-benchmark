using CreditCardApi.Data;
using Microsoft.EntityFrameworkCore;

namespace CreditCardApi.Data.Repositories;

public abstract class RepositoryBase<T> : IRepository<T> where T : class
{
    protected readonly CreditCardDbContext _context;
    protected readonly DbSet<T> _dbSet;

    protected RepositoryBase(CreditCardDbContext context)
    {
        _context = context;
        _dbSet = context.Set<T>();
    }

    public virtual async Task<IEnumerable<T>> GetAllAsync()
        => await _dbSet.ToListAsync();

    public virtual async Task<T?> GetByIdAsync(int id)
        => await _dbSet.FindAsync(id);

    public virtual async Task<T> AddAsync(T entity)
    {
        await _dbSet.AddAsync(entity);
        await _context.SaveChangesAsync();
        return entity;
    }

    public virtual async Task<T> UpdateAsync(T entity)
    {
        _dbSet.Update(entity);
        await _context.SaveChangesAsync();
        return entity;
    }

    public virtual async Task DeleteAsync(T entity)
    {
        _dbSet.Remove(entity);
        await _context.SaveChangesAsync();
    }
}
