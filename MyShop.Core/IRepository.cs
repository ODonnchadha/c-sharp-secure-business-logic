using System.Linq.Expressions;

namespace MyShop.Core;

public interface IRepository<T>
{
    ValueTask<T> AddAsync(T entity);
    ValueTask<T?> GetAsync(Guid id);

    Task<T> UpdateAsync(T entity);
    Task<IEnumerable<T>> AllAsync();
    Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate);
    Task<T?> GetAsync(Expression<Func<T, bool>> predicate);
    Task SaveChangesAsync();
}