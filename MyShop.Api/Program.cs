using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using MyShop.Core;
using MyShop.Domain.Models;
using MyShop.Infrastructure;
using MyShop.Infrastructure.Repositories;
using MyShop.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddDbContext<ShoppingContext>();
builder.Services.AddTransient<IRepository<Product>, ProductRepository>();
builder.Services.AddTransient<IRepository<Order>, OrderRepository>();
builder.Services.AddTransient<IRepository<Customer>, CustomerRepository>();
builder.Services.AddTransient<PaymentService>();
builder.Services.AddTransient<OrderProcessor>();
builder.Services.AddTransient<IShippingService, ShippingService>();

builder.Services.AddRateLimiter(rateLimiter =>
{
    rateLimiter.AddFixedWindowLimiter("ProductLimiter",
       options => {
           options.PermitLimit = 10;
           options.Window = TimeSpan.FromSeconds(5);
           options.QueueLimit = 10;
           options.QueueProcessingOrder 
            = System.Threading.RateLimiting.QueueProcessingOrder.OldestFirst;
       });
});

var app = builder.Build();

app.UseRateLimiter();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    // In real-world production applications you have to be very careful when/where you apply your migrations!
    using var scope = app.Services.CreateScope();

    var database = scope.ServiceProvider.GetRequiredService<ShoppingContext>();

    await database.Database.EnsureDeletedAsync();

    await database.Database.MigrateAsync();

    // Seed some demo data
    var repository = scope.ServiceProvider.GetRequiredService<IRepository<Product>>();
    var products = await repository.AllAsync();


    if (!products.Any())
    {
        await repository.AddAsync(new() { ProductId = Guid.Parse("d2dae150-9a29-4c78-b912-68ed66b056f0"), Name = "Camera", Price = 4990.99m, Stock = 12 });
        await repository.AddAsync(new() { Name = "Microphone", Price = 199.99m, Stock = 3 });
        await repository.AddAsync(new() { Name = "Laptop", Price = 2999.99m, Stock = 1 });
    }

    await repository.SaveChangesAsync();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
