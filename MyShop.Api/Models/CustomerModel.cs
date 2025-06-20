namespace MyShop.Api.Models;

public class CustomerModel
{
    public required string Email { get; init; }
    public required string Name { get; init; }
    public required string ShippingAddress { get; init; }
    public required string City { get; init; }
    public required string Country { get; init; }
    public required string PostalCode { get; init; }
}
