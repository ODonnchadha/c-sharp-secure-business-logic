using Microsoft.EntityFrameworkCore;
using MyShop.Domain.Models;
using System.Linq.Expressions;

namespace MyShop.Infrastructure.Repositories;

public class PaymentRepository(ShoppingContext context) : GenericRepository<Payment>(context)
{
    public override async Task<IEnumerable<Payment>> FindAsync(Expression<Func<Payment, bool>> predicate)
    {
        return await Context.Payments
            .Include(payment => payment.Order)
            .Where(predicate).ToListAsync();
    }

    public override async Task<Payment> UpdateAsync(Payment entity)
    {
        var payment = await Context.Payments
            .Include(p => p.Order)
            .SingleAsync(p => p.PaymentId == entity.PaymentId);

        payment.Status = entity.Status;

        return await base.UpdateAsync(payment);
    }
}
