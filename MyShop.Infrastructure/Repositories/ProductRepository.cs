using MyShop.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace MyShop.Infrastructure.Repositories;

public class ProductRepository(ShoppingContext context) : GenericRepository<Product>(context)
{
    public override async Task<Product> UpdateAsync(Product entity)
    {
        var product = await Context.Products
            .SingleAsync(p => p.ProductId == entity.ProductId);

        product.Price = entity.Price;
        product.Name = entity.Name;
        product.Stock = entity.Stock;

        return await base.UpdateAsync(product);
    }
}