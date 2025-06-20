using MyShop.Domain.Models;

namespace MyShop.Core;

public class OrderProcessor(
    IRepository<Product> productRepository,
    IRepository<Order> orderRepository,
    IShippingService shippingService)
{
    public bool TryReserveProductInStock(Guid productId, Customer customer)
    {
        try
        {
            var validCustomer = ValidateCustomer(customer);

            if (!validCustomer) return false;

            if (Inventory.Stock < 0) return false;

            Inventory.Stock -= 1;

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

    public async Task<Result> Reserve(Guid productId, Customer customer)
    {
        var customerValidationTask = ValidateCustomerAsync(customer);

        var customerIsValid = await customerValidationTask;

        if (!customerIsValid) return new Failure<Customer>(customer, Error.Validation, ["Customer data is not valid"]);

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
}
