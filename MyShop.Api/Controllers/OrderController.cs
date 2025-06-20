using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using MyShop.Api.Models;
using MyShop.Core;
using MyShop.Domain.Models;
using MyShop.Infrastructure.Services;

namespace MyShop.Api.Controllers;

[ApiController]
[Route("[controller]")]
public class OrderController(IRepository<Product> productRepository,
    IRepository<Order> orderRepository,
    IRepository<Customer> customerRepository,
    PaymentService paymentService,
    OrderProcessor orderProcessor) : ControllerBase
{
    [HttpPost("reserve-static/{productId}")]
    public Results<Ok<int>, BadRequest, NotFound> ReserveStatic(Guid productId)
    {
        var productReserved = orderProcessor.TryReserveProductInStock(productId, FakeCustomerData());

        if(!productReserved) return TypedResults.BadRequest();

        return TypedResults.Ok(Inventory.Stock);
    }

    [HttpPost("reserve/{productId}")]
    public async Task<Results<Ok<Order>, BadRequest, NotFound>> Reserve(Guid productId)
    {
        var reservationTask 
            = orderProcessor.Reserve(productId, FakeCustomerData());

        var reservationResult = await reservationTask;

        if(reservationResult is Failure { Error: Error.NotFound })
        {
            return TypedResults.NotFound();
        }
        else if (reservationResult is Failure<Product> failure)
        {
            return TypedResults.BadRequest();
        }
        else if (reservationResult is Success<Order> result)
        {
            return TypedResults.Ok(result.Value);
        }

        return TypedResults.BadRequest();
    }

    [HttpPost("create/{orderId}")]
    public async Task<Ok<Order>> CreateOrder(PlaceOrderModel orderModel)
    {
        // Exercise: Move this to the order processor and refactor the code.
        // Apply thread safe principles and best practices

        var customer = await customerRepository.GetAsync(c => c.Email == orderModel.Customer.Email);
        if (customer is null)
        {
            customer = new()
            {
                Name = orderModel.Customer.Name,
                Email = orderModel.Customer.Email,
                City = orderModel.Customer.City,
                Country = orderModel.Customer.Country,
                ShippingAddress = orderModel.Customer.ShippingAddress,
                PostalCode = orderModel.Customer.PostalCode
            };

            await customerRepository.AddAsync(customer);
        }

        var order = new Order
        {
            Customer = customer,
            Status = OrderStatus.Open
        };

        foreach (var cartItem in orderModel.Items)
        {
            var product = await productRepository.GetAsync(cartItem.ProductId);

            if (product is null) continue;

            var lineItem = new LineItem
            {
                Product = product,
                Quantity = cartItem.Quantity,
                Order = order
            };

            order.LineItems.Add(lineItem);
        }

        if (orderModel.PaymentType == PaymentType.Invoice)
        {
            order.Status = OrderStatus.Shipped;
        }
        else
        {
            order.Status = OrderStatus.WaitingForPayment;
        }

        await orderRepository.AddAsync(order);

        await orderRepository.SaveChangesAsync();

        return TypedResults.Ok(order);
    }

    [HttpPost("finalize/{orderId}")]
    public async Task<Results<Ok<Order>, BadRequest>> FinalizeOrder(Guid orderId, string paymentReference)
    {
        // Exercise: Move this to the order processor and refactor the code.
        // Apply thread safe principles and best practices

        var order = await orderRepository.GetAsync(orderId);

        if (order is null) return TypedResults.BadRequest();

        paymentService.ValidatePayment(paymentReference); // Bug?

        foreach (var item in order.LineItems)
        {
            var product = await productRepository.GetAsync(item.ProductId);
            
            if(product is null) continue;

            product.Stock -= item.Quantity;

            await productRepository.UpdateAsync(product);
        }

        order.Status = OrderStatus.Shipped;

        await orderRepository.UpdateAsync(order);

        await orderRepository.SaveChangesAsync();

        return TypedResults.Ok(order);
    }

    // This demo code is just used to make it easier to test
    private static Customer FakeCustomerData()
    {
        return new() { Name = "Demo", City = "Demo", Country = "Sweden", Email = $"{Guid.NewGuid()}@ekberg.dev", PostalCode = "Demo", ShippingAddress = "Demo" };
    }
}