namespace MyShop.Core;

public interface IShippingService
{
    ValueTask<bool> ValidateShippingAddressAsync(string shippingAddres, string postalCode, string country);
    bool ValidateShippingAddress(string shippingAddres, string postalCode, string country);
}
