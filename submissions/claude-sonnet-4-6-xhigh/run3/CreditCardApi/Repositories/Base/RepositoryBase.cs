using CreditCardApi.Data;
using Microsoft.EntityFrameworkCore;

namespace CreditCardApi.Repositories.Base;

public abstract class RepositoryBase<T> : IRepositoryBase<T> where T : class
{
    protected readonly AppDbContext Context;

    protected RepositoryBase(AppDbContext context)
    {
        Context = context;
    }

    public virtual async Task<IEnumerable<T>> GetAllAsync()
        => await Context.Set<T>().ToListAsync();

    public virtual async Task<T?> GetByIdAsync(int id)
        => await Context.Set<T>().FindAsync(id);

    public async Task AddAsync(T entity)
        => await Context.Set<T>().AddAsync(entity);

    public void Update(T entity)
        => Context.Set<T>().Update(entity);

    public void Delete(T entity)
        => Context.Set<T>().Remove(entity);

    public async Task SaveChangesAsync()
        => await Context.SaveChangesAsync();
}
