using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using MyShop.Core;
using MyShop.Domain.Models;
using MyShop.Infrastructure.Repositories;

namespace MyShop.Api.Controllers;


[ApiController]
[Route("[controller]")]
public class ProductController(IRepository<Product> productRepository)
{
    [HttpGet]
    public async Task<Ok<IEnumerable<Product>>> Products()
    {
        return TypedResults.Ok(await productRepository.AllAsync());
    }
}
