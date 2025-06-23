using MyShop.Domain.Models;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;

namespace MyShop.Core;

public class OrderProcessor(IRepository<Product> productRepository,
    IRepository<Order> orderRepository,
    IShippingService shippingService)
{
    private static Lock reserveStockLock = new();
    private static SemaphoreSlim reserveStockSemaphore = new(1);

    public bool TryReserveProductInStock(Guid productId, Customer customer)
    {
        try
        {
            var validCustomer = ValidateCustomer(customer);

            if (!validCustomer) return false;

            lock (reserveStockLock)
            {
                // Only one at a time please!

                if (Inventory.Stock <= 0) return false;

                Thread.Sleep(5); // Fake that it took some time to reserve it

                Inventory.Stock = Inventory.Stock - 1;
                // Interlocked.Decrement(ref Inventory.Stock);
            }

            var order = new Order
            {
                Status = OrderStatus.FullyPaid,
                Customer = customer
            };

            Save(order);

            return true;
        }
        catch (Exception ex)
        {
            // Roll back?
        }
        finally
        {
            // Clean up resources
        }
        return false;
    }

    private static AsyncLocal<Customer> currentCustomer = new();
    public async Task<Result> Reserve(Guid productId, Customer customer)
    {
        var customerValidationTask = ValidateCustomerAsync(customer);

        var customerIsValid = await customerValidationTask;

        if (currentCustomer.Value is not null) throw new Exception();

        currentCustomer.Value = customer;

        if (!customerIsValid) return new Failure<Customer>(customer, Error.Validation, ["Customer data is not valid"]);

        await reserveStockSemaphore.WaitAsync();

        try
        {
            #region Only one thread at a time please
            var product = await productRepository.GetAsync(productId);

            if (product is null)
            {
                return new Failure(Error.NotFound);
            }

            if (product.Stock <= 0)
            {
                return new Failure<Product>(product, Error.Validation, ["Out of stock"]);
            }

            var order = new Order
            {
                Status = OrderStatus.WaitingForPayment,
                Customer = customer
            };

            await orderRepository.UpdateAsync(order);

            await UpdateInventoryForAsync(product);

            await orderRepository.SaveChangesAsync();

            return new Success<Order>(order);
            #endregion
        }
        finally
        {
            reserveStockSemaphore.Release();
        }
    }

    private bool ValidateCustomer(Customer customer)
    {
        // This logic could be improved with returning what was invalid
        if (customer is null) return false;
        if (customer.Name.Length < 2) return false;
        if (string.IsNullOrWhiteSpace(customer.Email)) return false;
        if (string.IsNullOrEmpty(customer.ShippingAddress)) return false;
        if (string.IsNullOrWhiteSpace(customer.Country)) return false;
        if (string.IsNullOrWhiteSpace(customer.PostalCode)) return false;

        var shippingServiceIsValid = shippingService.ValidateShippingAddress(
            customer.ShippingAddress,
            customer.PostalCode,
            customer.Country
        );

        if (!shippingServiceIsValid) return false;

        return true;
    }

    private async Task<bool> ValidateCustomerAsync(Customer customer)
    {
        // This logic could be improved with returning what was invalid
        if (customer is null) return false;
        if (customer.Name.Length < 2) return false;
        if (string.IsNullOrWhiteSpace(customer.Email)) return false;
        if (string.IsNullOrEmpty(customer.ShippingAddress)) return false;
        if (string.IsNullOrWhiteSpace(customer.Country)) return false;
        if (string.IsNullOrWhiteSpace(customer.PostalCode)) return false;

        var shippingServiceIsValid = await shippingService.ValidateShippingAddressAsync(
            customer.ShippingAddress,
            customer.PostalCode,
            customer.Country
        );

        if (!shippingServiceIsValid) return false;

        return true;
    }

    private async Task UpdateInventoryForAsync(Product product)
    {
        product.Stock -= 1;

        await productRepository.UpdateAsync(product);
    }

    private void Save(Order order)
    {
        // Not implemented
    }

    private static SemaphoreSlim throttle = new(5);
    public static async Task<string> 
        GenerateYearlyReportForAsync(IEnumerable<Order> orders)
    {
        await throttle.WaitAsync();
        Debug.WriteLine("Entering");
        try
        {
            var builder = new StringBuilder();
            builder.Append("orderId,date,hash");

            foreach (var order in orders)
            {
                var lineItemHash = Hash(string.Join("", order.LineItems.Select(l => Hash($"{l.ProductId}{l.Quantity}"))));
                var orderHash = Hash($"{order.OrderId}{lineItemHash}{order.OrderDate}");

                builder.AppendLine($"{order.OrderId}{order.OrderDate}{orderHash}");
            }

            return builder.ToString();

            string Hash(string data)
            {
                // Simulate extra load to hurt the CPU
                var time = DateTimeOffset.UtcNow;
                while(true)
                {
                    if((DateTimeOffset.UtcNow - time) > TimeSpan.FromMilliseconds(5))
                    {
                        break;
                    }

                    Thread.Sleep(0);
                }
                return Convert.ToBase64String(
                    SHA256.HashData(Encoding.UTF8.GetBytes(data))
                );
            }
        }
        finally
        {
            throttle.Release();
            Debug.WriteLine("Released");
        }
    }
}
