using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using MyShop.Core;

namespace MyShop.Infrastructure.Repositories;

public abstract class GenericRepository<T>(ShoppingContext context) : IRepository<T>
    where T : class
{
    protected ShoppingContext Context = context;

    public virtual async ValueTask<T> AddAsync(T entity)
    {
        var entry = await Context.AddAsync(entity);

        return entry.Entity;
    }

    public virtual async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate)
    {
        return await Context.Set<T>()
            .AsQueryable()
            .Where(predicate).ToListAsync();
    }

    public virtual async Task<T?> GetAsync(Expression<Func<T, bool>> predicate)
    {
        return await Context.Set<T>()
            .AsQueryable()
            .Where(predicate).FirstOrDefaultAsync();
    }

    public virtual async ValueTask<T?> GetAsync(Guid id)
    {
        return await Context.FindAsync<T>(id);
    }

    public virtual async Task<IEnumerable<T>> AllAsync()
    {
        return await Context.Set<T>().ToListAsync();
    }

    public virtual Task<T> UpdateAsync(T entity)
    {
        // EF does not support updating asynchronously, but this enables subclasses to fist fetch the element asynchronously.
        return Task.FromResult(Context.Update(entity)
            .Entity);
    }

    public async Task SaveChangesAsync()
    {
        await Context.SaveChangesAsync();
    }
}