using Microsoft.EntityFrameworkCore;
using MyShop.Domain.Models;
using System.Linq.Expressions;

namespace MyShop.Infrastructure.Repositories;

public class OrderRepository(ShoppingContext context) : GenericRepository<Order>(context)
{
    public override async Task<IEnumerable<Order>> FindAsync(Expression<Func<Order, bool>> predicate)
    {
        return await Context.Orders
            .Include(order => order.Payments)
            .Include(order => order.LineItems)
            .ThenInclude(lineItem => lineItem.Product)
            .Where(predicate).ToListAsync();
    }

    public override async Task<Order> UpdateAsync(Order entity)
    {
        var order = await Context.Orders
            .Include(o => o.LineItems)
            .ThenInclude(lineItem => lineItem.Product)
            .FirstOrDefaultAsync(o => o.OrderId == entity.OrderId);
        
        if (order is null)
        {
            return await base.AddAsync(entity);
        }

        order.OrderDate = entity.OrderDate;
        order.LineItems = entity.LineItems;

        return await base.UpdateAsync(order);
    }
}
