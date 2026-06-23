using CreditCardApi.Data;
using Microsoft.EntityFrameworkCore;

namespace CreditCardApi.Repositories.Base;

public class RepositoryBase<T>(AppDbContext context) : IRepositoryBase<T> where T : class
{
    protected readonly AppDbContext Context = context;

    public virtual async Task<T?> GetByIdAsync(int id)
    {
        return await Context.Set<T>().FindAsync(id);
    }

    public virtual async Task<IEnumerable<T>> GetAllAsync()
    {
        return await Context.Set<T>().ToListAsync();
    }

    public virtual async Task<T> AddAsync(T entity)
    {
        await Context.Set<T>().AddAsync(entity);
        return entity;
    }

    public virtual async Task<T> UpdateAsync(T entity)
    {
        Context.Set<T>().Update(entity);
        return await Task.FromResult(entity);
    }

    public virtual async Task DeleteAsync(T entity)
    {
        Context.Set<T>().Remove(entity);
        await Task.CompletedTask;
    }

    public async Task SaveChangesAsync()
    {
        await Context.SaveChangesAsync();
    }
}
