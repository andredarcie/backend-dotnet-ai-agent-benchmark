using Microsoft.EntityFrameworkCore;

namespace CreditCardApi.Data.Repositories;

public class RepositoryBase<T> : IRepository<T> where T : class
{
    protected readonly CreditCardDbContext Context;
    protected readonly DbSet<T> DbSet;

    public RepositoryBase(CreditCardDbContext context)
    {
        Context = context;
        DbSet = context.Set<T>();
    }

    public virtual async Task<T?> GetByIdAsync(int id)
    {
        return await DbSet.FindAsync(id);
    }

    public virtual async Task<IEnumerable<T>> GetAllAsync()
    {
        return await DbSet.ToListAsync();
    }

    public virtual async Task<T> AddAsync(T entity)
    {
        var result = await DbSet.AddAsync(entity);
        return result.Entity;
    }

    public virtual async Task UpdateAsync(T entity)
    {
        DbSet.Update(entity);
        await Task.CompletedTask;
    }

    public virtual async Task DeleteAsync(T entity)
    {
        DbSet.Remove(entity);
        await Task.CompletedTask;
    }

    public virtual async Task SaveChangesAsync()
    {
        await Context.SaveChangesAsync();
    }
}
