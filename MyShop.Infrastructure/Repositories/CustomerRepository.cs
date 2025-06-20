using Microsoft.EntityFrameworkCore;
using MyShop.Domain.Models;

namespace MyShop.Infrastructure.Repositories
{
    public class CustomerRepository(ShoppingContext context) : GenericRepository<Customer>(context)
    {
        public override async Task<Customer> UpdateAsync(Customer entity)
        {
            var customer = await Context.Customers
                .SingleAsync(c => c.CustomerId == entity.CustomerId);

            customer.Name = entity.Name;
            customer.Email = entity.Email;
            customer.City = entity.City;
            customer.PostalCode = entity.PostalCode;
            customer.ShippingAddress = entity.ShippingAddress;
            customer.Country = entity.Country;

            return await base.UpdateAsync(customer);
        }
    }
}
