using MyShop.Core;

namespace MyShop.Infrastructure.Services;

public class ShippingService : IShippingService
{
    public async ValueTask<bool> ValidateShippingAddressAsync(string shippingAddres, string postalCode, string country)
    {
        await Task.Delay(3000);

        return true;
    }

    public bool ValidateShippingAddress(string shippingAddres, string postalCode, string country)
    {
        // Simulate using a blocking service
        Thread.Sleep(1);

        return true;
    }
}